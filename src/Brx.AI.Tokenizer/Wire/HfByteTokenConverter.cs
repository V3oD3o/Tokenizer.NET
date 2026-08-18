using Brx.AI.Tokenizer.Bpe;
using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class HfByteTokenConverter : IBlockConverter<byte, int>
{
   private readonly BpeVocab _vocab;

   public HfByteTokenConverter(BpeVocab vocab)
   {
      _vocab = vocab;
   }

   public void Convert(
       ReadOnlySpan<byte> input,
       ref int inputIndex,
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      int inputLength = input.Length;
      int outputLength = output.Length;

      while (inputIndex < inputLength)
      {
         byte b = input[inputIndex];
         int id = _vocab.GetByteTokenId(b);

         if (outputIndex >= outputLength)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = id;
         inputIndex++;
      }
   }

   public void Flush(
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;
      // No state -> nothing to flush
   }
}
