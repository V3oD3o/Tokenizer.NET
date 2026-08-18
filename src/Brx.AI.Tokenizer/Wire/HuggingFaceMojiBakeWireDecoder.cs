using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class HuggingFaceMojiBakeWireDecoder : IBlockConverter<byte, char>
{
   public void Convert(
      ReadOnlySpan<byte> input,
      ref int inputIndex,
      Span<char> output,
      ref int outputIndex,
      out bool completed)
   {
      completed = true;

      int inputLength = input.Length;
      int outputLength = output.Length;

      while (inputIndex < inputLength)
      {
         byte b = input[inputIndex];
         char ch = HuggingFaceByteMapping.Forward[b];

         if (outputIndex >= outputLength)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = ch;
         inputIndex++;
      }
   }

   public void Flush(
      Span<char> output,
      ref int outputIndex,
      out bool completed)
   {
      completed = true;
   }
}
