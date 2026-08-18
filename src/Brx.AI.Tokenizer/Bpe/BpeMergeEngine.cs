using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Bpe;

public sealed class BpeMergeEngine : IBlockConverter<int, int>
{
   private readonly IBpeMergeTable _merges;

   private int _pendingLeft;
   private bool _hasPendingLeft;

   public BpeMergeEngine(IBpeMergeTable merges)
   {
      _merges = merges;
   }

   public void Convert(
       ReadOnlySpan<int> input,
       ref int inputIndex,
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      while (inputIndex < input.Length)
      {
         int right = input[inputIndex];

         if (!_hasPendingLeft)
         {
            _pendingLeft = right;
            _hasPendingLeft = true;
            inputIndex++;
            continue;
         }

         int left = _pendingLeft;

         bool hasCurrent = _merges.TryGetMergedId(left, right, out int currentMerged, out int currentPriority);
         if (!hasCurrent) currentPriority = int.MaxValue;

         int forwardPriority = int.MaxValue;
         int forwardMerged = 0;

         if (inputIndex + 1 < input.Length)
         {
            int next = input[inputIndex + 1];
            if (_merges.TryGetMergedId(right, next, out forwardMerged, out forwardPriority))
            {
            }
         }

         if (forwardPriority < currentPriority)
         {
            int mergedRight = forwardMerged;
            inputIndex += 2;

            if (_merges.TryGetMergedId(left, mergedRight, out int newMerged, out int newPriority))
            {
               _pendingLeft = newMerged;
               continue;
            }

            if (outputIndex >= output.Length)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = left;
            _pendingLeft = mergedRight;
            _hasPendingLeft = true;
            continue;
         }

         if (hasCurrent)
         {
            _pendingLeft = currentMerged;
            inputIndex++;
            continue;
         }

         if (outputIndex >= output.Length)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = left;
         _pendingLeft = right;
         _hasPendingLeft = true;
         inputIndex++;
      }
   }

   public void FeedToken(
       int tokenId,
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      if (!_hasPendingLeft)
      {
         _pendingLeft = tokenId;
         _hasPendingLeft = true;
         return;
      }

      int left = _pendingLeft;
      int right = tokenId;

      bool hasCurrent = _merges.TryGetMergedId(left, right, out int currentMerged, out int currentPriority);
      if (!hasCurrent) currentPriority = int.MaxValue;

      int forwardPriority = int.MaxValue;
      int forwardMerged = 0;

      // FeedToken has no lookahead, so forward merge cannot be attempted.
      // It behaves like classic BPE for single-token streaming.
      // Forward-looking behavior only happens in Convert(), where lookahead exists.

      if (hasCurrent)
      {
         _pendingLeft = currentMerged;
         return;
      }

      if (outputIndex >= output.Length)
      {
         completed = false;
         return;
      }

      output[outputIndex++] = left;
      _pendingLeft = right;
      _hasPendingLeft = true;
   }

   public void Flush(
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      if (_hasPendingLeft)
      {
         if (outputIndex >= output.Length)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = _pendingLeft;
         _hasPendingLeft = false;
      }

      completed = true;
   }
}
