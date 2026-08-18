using System;
using System.Runtime.CompilerServices;

namespace Brx.Hpc.Blocks;

public sealed class ConversionBlockIteration<TIn, TOut> : IBlockIteration<TOut>
{
   private readonly IBlockIteration<TIn> _input;
   private readonly IBlockAllocator _allocator;
   private readonly int _blockSize;

   private readonly IBlockConverter<TIn, TOut> _converter;
   private readonly IPassThroughBlockConverter<TIn>? _passThroughConverter;

   // Minimum chunk enforcement
   private const int MinChunk = 16;              // minimum useful chunk
   private const int MinBlockSizeForChunk = 128; // only enforce when block is >= 128

   public ConversionBlockIteration(
       IBlockIteration<TIn> input,
       IBlockConverter<TIn, TOut> converter,
       IBlockAllocator allocator,
       int blockSize)
   {
      ArgumentNullException.ThrowIfNull(input);
      ArgumentNullException.ThrowIfNull(converter);
      ArgumentNullException.ThrowIfNull(allocator);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockSize);

      _input = input;
      _allocator = allocator;
      _blockSize = blockSize;

      _converter = converter;
      _passThroughConverter = converter as IPassThroughBlockConverter<TIn>;
   }

   public bool CanReiterate => _input.CanReiterate;
   public bool HasTotalLength => false;
   public long TotalLength => throw new NotSupportedException("ConversionBlockIteration cannot compute total length in advance.");

   public IEnumerable<BlockHandle<TOut>> GetBlocks(int maxConcurrentBlocks)
   {
      ArgumentOutOfRangeException.ThrowIfNegative(maxConcurrentBlocks);

      int poolCapacity = maxConcurrentBlocks + 1;
      IBlockPool<TOut> pool = _allocator.CreatePool<TOut>(poolCapacity);

      BlockHandle<TOut> outHandle = pool.Allocate(_blockSize);
      int outCount = 0;

      int minRemaining = (_blockSize >= MinBlockSizeForChunk) ? MinChunk : 1;
      bool forceNewOutputBlock = false;

      foreach (var inBlock in _input.GetBlocks(0))
      {
         if (inBlock.IsEmpty())
         {
            continue;
         }

         // Fast path: pass-through if converter supports it and block is safe
         if (_passThroughConverter != null && _passThroughConverter.CanPassThrough(inBlock.Span, 0))
         {
            if (outCount > 0)
            {
               pool.SetLength(outHandle, outCount);
               yield return outHandle;

               outCount = 0;
               forceNewOutputBlock = true;
            }

            var tmpBlock = inBlock;
            var passThroughBlock = Unsafe.As<BlockHandle<TIn>, BlockHandle<TOut>>(ref tmpBlock);
            yield return passThroughBlock;
            continue;
         }

         int inIndex = 0;
         int inLength = inBlock.Length;
         while (inIndex < inLength)
         {
            if (forceNewOutputBlock || (_blockSize - outCount < minRemaining))
            {
               if (outCount > 0)
               {
                  pool.SetLength(outHandle, outCount);
                  yield return outHandle;
               }

               if (!pool.HasFreeSlots)
               {
                  yield return BlockHandle<TOut>.Empty;

                  if (!pool.HasFreeSlots)
                     throw new InvalidOperationException("Block pool is exhausted and the caller did not release any blocks.");
               }

               forceNewOutputBlock = false;
               outHandle = pool.Allocate(_blockSize);
               outCount = 0;
            }

            int inBefore = inIndex;
            int outBefore = outCount;

            _converter.Convert(
                inBlock.Span,
                ref inIndex,
                pool.GetWritableSpan(outHandle),
                ref outCount,
                out bool completed);

            if (inIndex == inBefore && outCount == outBefore)
            {
               if (!completed && outCount > 0)
               {
                  // retry Convert with a fresh output block
                  forceNewOutputBlock = true;
                  continue; 
               }

               throw new InvalidOperationException("IBlockConverter.Convert made no progress while reporting incomplete.");
            }
         }

         inBlock.Release();
      }

      while (true)
      {
         if (forceNewOutputBlock || (_blockSize - outCount <= 0))
         {
            if (outCount > 0)
            {
               pool.SetLength(outHandle, outCount);
               yield return outHandle;
            }

            if (!pool.HasFreeSlots)
            {
               yield return BlockHandle<TOut>.Empty;

               if (!pool.HasFreeSlots)
                  throw new InvalidOperationException("Block pool is exhausted and the caller did not release any blocks.");
            }

            forceNewOutputBlock = false;
            outHandle = pool.Allocate(_blockSize);
            outCount = 0;
         }

         int outBefore = outCount;

         _converter.Flush(pool.GetWritableSpan(outHandle), ref outCount, out bool flushCompleted);

         if (flushCompleted)
            break;

         if (outCount == outBefore)
         {
            if (outCount > 0)
            {
               // retry Flush with a fresh output block
               forceNewOutputBlock = true;
               continue;
            }

            throw new InvalidOperationException("IBlockConverter.Flush made no progress while reporting incomplete.");
         }

         pool.SetLength(outHandle, outCount);
         yield return outHandle;

         if (!pool.HasFreeSlots)
            yield return BlockHandle<TOut>.Empty;

         outHandle = pool.Allocate(_blockSize);
         outCount = 0;
      }

      if (outCount > 0)
      {
         pool.SetLength(outHandle, outCount);
         yield return outHandle;
      }
      else
      {
         outHandle.Release();
      }
   }
}
