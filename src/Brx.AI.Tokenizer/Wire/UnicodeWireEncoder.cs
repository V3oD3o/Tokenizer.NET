using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class UnicodeWireEncoder : IBlockConverter<char, byte>
{
   public UnicodeWireEncoder()
   {
   }

   public void Convert(
       ReadOnlySpan<char> input,
       ref int inputIndex,
       Span<byte> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      int inputLength = input.Length;
      int outputLength = output.Length;

      while (inputIndex < inputLength)
      {
         char ch = input[inputIndex];
         ushort value = ch;

         if (outputIndex + 1 >= outputLength)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = (byte)(value & 0xFF);
         output[outputIndex++] = (byte)(value >> 8);

         inputIndex++;
      }
   }

   public void Flush(Span<byte> output, ref int outputIndex, out bool completed)
   {
      completed = true;
   }
}
