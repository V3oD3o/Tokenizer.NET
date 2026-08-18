using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public class AsciiSubsetWireDecoder : IBlockConverter<byte, char>
{
   private readonly bool[] _allowed;
   private readonly WireFallback _fallback;

   public AsciiSubsetWireDecoder(string alphabet, WireFallback fallback)
   {
      ArgumentNullException.ThrowIfNull(fallback);

      _allowed = AsciiSubsetWireEncoder.CreateAllowedTable(alphabet);
      _fallback = fallback;
   }

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

         if (_allowed[b])
         {
            if (outputIndex >= outputLength)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = (char)b;
            inputIndex++;
            continue;
         }

         char ch;
         if (!_fallback.TryMap(b, out ch))
         {
            inputIndex++;
            continue;
         }

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
