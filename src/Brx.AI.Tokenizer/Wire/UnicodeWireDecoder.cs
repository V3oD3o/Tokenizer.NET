using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class UnicodeWireDecoder : IBlockConverter<byte, char>
{
   private bool _hasPending;
   private byte _pending;
   private readonly WireFallback _fallback;

   public UnicodeWireDecoder(WireFallback fallback)
   {
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
         if (!_hasPending)
         {
            _pending = input[inputIndex++];
            _hasPending = true;
            continue;
         }

         byte lo = _pending;
         byte hi = input[inputIndex++];
         _hasPending = false;

         char ch = (char)(lo | hi << 8);

         if (outputIndex >= outputLength)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = ch;
      }
   }

   public void Flush(
      Span<char> output,
      ref int outputIndex,
      out bool completed)
   {
      if (_hasPending)
      {
         char ch;
         if (_fallback.TryMap(_pending, out ch))
         {
            if (outputIndex >= output.Length)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = ch;
         }

         _hasPending = false;
      }

      completed = true;
   }
}
