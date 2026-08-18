using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class MojiBakeWireEncoder : IBlockConverter<char, byte>
{
   private readonly WireFallback _fallback;

   public MojiBakeWireEncoder(WireFallback fallback)
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

         if (ch <= 0x00FF)
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

         // fallback
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
                $"Character U+{(int)ch:X4} cannot be encoded in MojiBake.");

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
