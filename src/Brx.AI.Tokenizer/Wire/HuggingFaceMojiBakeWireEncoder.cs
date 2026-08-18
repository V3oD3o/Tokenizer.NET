using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class HuggingFaceMojiBakeWireEncoder : IBlockConverter<char, byte>
{
   private readonly WireFallback _fallback;

   public HuggingFaceMojiBakeWireEncoder(WireFallback fallback)
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

      var reverse = HuggingFaceByteMapping.Reverse;

      int inputLength = input.Length;
      int outputLength = output.Length;
      int reverseLength = reverse.Length;
      
      while (inputIndex < inputLength)
      {
         char ch = input[inputIndex];
         int code = ch;

         if (code < reverseLength)
         {
            byte b = reverse[code];

            if (outputIndex >= outputLength)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = b;
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
                $"Character U+{(int)ch:X4} cannot be encoded in HuggingFaceMojiBake.");

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
