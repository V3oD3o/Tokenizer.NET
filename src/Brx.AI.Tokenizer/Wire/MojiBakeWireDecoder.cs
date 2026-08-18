using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class MojiBakeWireDecoder : IBlockConverter<byte, char>
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
         char ch = (char)input[inputIndex];

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
