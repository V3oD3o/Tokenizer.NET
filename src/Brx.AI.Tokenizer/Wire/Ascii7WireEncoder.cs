using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class Ascii7WireEncoder : IBlockConverter<char, byte>
{
   private readonly WireFallback _fallback;

   public Ascii7WireEncoder(WireFallback fallback)
   {
      ArgumentNullException.ThrowIfNull(fallback);
      _fallback = fallback;
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

         if (ch <= 0x7F)
         {
            if (outputIndex >= outputLength)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = (byte)ch;
            inputIndex++;
            continue;
         }

         if (!TryFallback(ch, out byte fb))
         {
            inputIndex++;
            continue;
         }

         if (outputIndex >= outputLength)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = fb;
         inputIndex++;
      }
   }

   public void Flush(Span<byte> output, ref int outputIndex, out bool completed)
   {
      completed = true;
   }

   private bool TryFallback(char ch, out byte b)
   {
      switch (_fallback.Mode)
      {
         case WireFallbackOp.Throw:
            throw new InvalidOperationException(
                $"Character U+{(int)ch:X4} cannot be encoded in Ascii7.");

         case WireFallbackOp.Skip:
            b = 0;
            return false;

         case WireFallbackOp.Replace:
            b = (byte)_fallback.Replacement;
            return true;
      }

      b = 0;
      return false;
   }
}
