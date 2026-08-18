using System.Diagnostics;

namespace Brx.Hpc.Blocks;

public sealed class StreamBlockIteration : IBlockIteration<byte>
{
   private readonly Stream _stream;
   private readonly IBlockAllocator _allocator;
   private readonly int _blockSize;
   private readonly long _initialPosition;

   private bool _firstIterationStarted;

   private long _totalLength;
   private bool _failedToFetchTotalLength;

   public bool CanReiterate => _stream.CanSeek;
   
   public bool HasTotalLength => _stream.CanSeek || TryFetchTotalLength();

   public long TotalLength 
   {
      get
      {
         if (!TryFetchTotalLength())
         {
            throw new NotSupportedException();
         }
         return _totalLength;
      }
   }

   public StreamBlockIteration(Stream stream, IBlockAllocator allocator, int blockSize)
   {
      _stream = stream;
      _allocator = allocator;
      _blockSize = blockSize;

      // We only need the initial position when CanSeek == true
      _initialPosition = stream.CanSeek ? stream.Position : 0;

      // Mark total length as "unknown" initially
      _totalLength = -1;
   }

   public IEnumerable<BlockHandle<byte>> GetBlocks(int maxConcurrentBlocks)
   {
      // Reiteration logic
      if (!_firstIterationStarted)
      {
         _firstIterationStarted = true;
      }
      else if (_stream.CanSeek)
      {
         _stream.Seek(_initialPosition, SeekOrigin.Begin);
      }
      else
      {
         throw new NotSupportedException("This block iteration cannot be reiterated because the underlying stream is not seekable.");
      }

      // Iterator-owned pool sized cooperatively:
      // caller retention + iterator internal working set (1)
      int poolCapacity = maxConcurrentBlocks + 1;
      IBlockPool<byte> _pool = _allocator.CreatePool<byte>(poolCapacity);

      // EMA tuning parameters
      const double MaxLatencyForFastStream = 0.005;               //  5 ms
      const double MinThroughputForFastStream = 50 * 1024 * 1024; // 50 MB/s

      const double MinAlpha = 0.55;
      const double MaxAlpha = 0.95;

      const double MinBeta = 0.55;
      const double MaxBeta = 0.90;

      while (true)
      {
         // --- Cooperative retention exhaustion check BEFORE allocation ---
         if (!_pool.HasFreeSlots)
         {
            // Signal pool exhaustion ONCE
            yield return BlockHandle<byte>.Empty;

            // Caller may release retained blocks -> re-check
            if (!_pool.HasFreeSlots)
            {
               // Caller did NOT release -> cooperative contract violated
               throw new InvalidOperationException("Block pool is exhausted and the caller did not release any blocks.");
            }

            // Caller released some blocks -> continue normally
         }

         // Safe to allocate now
         BlockHandle<byte> handle = _pool.Allocate(_blockSize);
         Span<byte> span = _pool.GetWritableSpan(handle);

         // EMA state
         double ema = double.NaN;

         double alpha = 0.80;
         double beta = 0.75;
         double oneMinusAlpha = 1.0 - alpha;

         long lastTimestamp = Stopwatch.GetTimestamp();
         long bytesSinceLast = 0;

         int filled = 0;

         // --- Adaptive fill loop ---
         while (filled < _blockSize)
         {
            long before = Stopwatch.GetTimestamp();
            int read = _stream.Read(span.Slice(filled));
            long after = Stopwatch.GetTimestamp();

            if (read == 0)
            {
               if (filled > 0)
               {
                  // Last block completion
                  _pool.SetLength(handle, filled);
                  yield return handle;
               }
               else
               {
                  // Release empty block
                  handle.Release();
               }
               yield break;
            }

            filled += read;
            bytesSinceLast += read;

            // Latency measurement
            double latency = (after - before) / (double)Stopwatch.Frequency;
            double normLatency = Math.Min(1.0, latency / MaxLatencyForFastStream);

            // Throughput measurement
            long now = after;
            double elapsed = (now - lastTimestamp) / (double)Stopwatch.Frequency;
            double throughput = bytesSinceLast / Math.Max(elapsed, 1e-9);
            double normThroughput = Math.Min(1.0, throughput / MinThroughputForFastStream);

            // Auto-tune α
            alpha = MinAlpha + (MaxAlpha - MinAlpha) * (1.0 - normLatency);
            alpha = Math.Clamp(alpha, MinAlpha, MaxAlpha);
            oneMinusAlpha = 1.0 - alpha;

            // Auto-tune β
            beta = MinBeta + (MaxBeta - MinBeta) * normThroughput;
            beta = Math.Clamp(beta, MinBeta, MaxBeta);

            // EMA update
            ema = double.IsNaN(ema) ? read : ema * alpha + read * oneMinusAlpha;

            int remaining = _blockSize - filled;
            int threshold = (int)(ema * beta);

            if (remaining < threshold)
               break;

            // Reset throughput window every 10ms
            if (elapsed >= 0.010)
            {
               lastTimestamp = now;
               bytesSinceLast = 0;
            }
         }

         // Normal block completion
         _pool.SetLength(handle, filled);
         yield return handle;

         // Caller must call handle.Release()
      }
   }

   private bool TryFetchTotalLength()
   {
      if (_totalLength < 0)
      {
         if (_failedToFetchTotalLength)
         {
            return false;
         }

         try
         {
            _totalLength = _stream.Length;
         }
         catch (Exception ex) when (ex is NotSupportedException || ex is NotImplementedException || ex is InvalidOperationException)
         {
            _failedToFetchTotalLength = true;
            return false;
         }

         if (_totalLength < 0)
         {
            _failedToFetchTotalLength = true;
            return false;
         }
      }
      return true;
   }
}
