namespace Brx.AI.Tokenizer.Bpe;

public sealed class BpeTokenizer
{
   private readonly BpeVocab _vocab;
   private readonly BpeMergeTable _merges;

   public BpeTokenizer(BpeVocab vocab, BpeMergeTable merges)
   {
      ArgumentNullException.ThrowIfNull(vocab);
      ArgumentNullException.ThrowIfNull(merges);

      _vocab = vocab;
      _merges = merges;
   }

   // ---------------------------------------------------------------------
   // Convenience API: List<int> in, caller-provided List<int> out
   // ---------------------------------------------------------------------
   public int MergeAll(IEnumerable<int> input, List<int> output)
   {
      ArgumentNullException.ThrowIfNull(input);
      ArgumentNullException.ThrowIfNull(output);

      var engine = new BpeMergeEngine(_merges);

      // Temporary buffer for streaming
      Span<int> buffer = stackalloc int[256];
      int bufIndex = 0;

      int totalWritten = 0;

      foreach (int token in input)
      {
         bool completed;

         do
         {
            engine.FeedToken(token, buffer, ref bufIndex, out completed);
            if (!completed)
            {
               FlushBuffer(buffer, ref bufIndex, output, ref totalWritten);
            }

         } while (!completed);
      }

      // Final flush
      bool finalCompleted;
      do
      {
         engine.Flush(buffer, ref bufIndex, out finalCompleted);

         if (!finalCompleted)
            FlushBuffer(buffer, ref bufIndex, output, ref totalWritten);

      } while (!finalCompleted);

      // Flush remaining
      FlushBuffer(buffer, ref bufIndex, output, ref totalWritten);

      return totalWritten;
   }

   private static void FlushBuffer(Span<int> buffer, ref int bufIndex, List<int> output, ref int totalWritten)
   {
      for (int i = 0; i < bufIndex; i++)
      {
         output.Add(buffer[i]);
         totalWritten++;
      }

      bufIndex = 0;
   }

   // ---------------------------------------------------------------------
   // Later: block-iterator integration
   // ---------------------------------------------------------------------
   // public IEnumerable<BlockHandle<int>> GetBlocks() { ... }
   // public bool CanReiterate => false;
   // public bool HasTotalLength => false;
   // public long TotalLength => throw new NotSupportedException();
}
