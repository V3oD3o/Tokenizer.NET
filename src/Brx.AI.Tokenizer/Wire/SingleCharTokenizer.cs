using Brx.AI.Tokenizer.Bpe;
using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class SingleCharTokenizer : IBlockConverter<char, int>
{
   private readonly BpeVocab _vocab;
   private readonly bool _useHuggingFaceByteLevelMapping;

   public SingleCharTokenizer(BpeVocab vocab, bool useHuggingFaceByteLevelMapping = false)
   {
      ArgumentNullException.ThrowIfNull(vocab);
      _vocab = vocab;
      _useHuggingFaceByteLevelMapping = useHuggingFaceByteLevelMapping;
   }

   public void Convert(
       ReadOnlySpan<char> input,
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
         char ch = input[inputIndex];
         if (_useHuggingFaceByteLevelMapping)
         {
            ch = HuggingFaceByteMapping.Forward[ch];
         }
         int id = _vocab.GetCharTokenId(ch);

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
      // Stateless -> nothing to flush
   }
}
