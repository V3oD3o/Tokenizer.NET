using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class AsciiSubsetWireEncoder : IBlockConverter<char, byte>
{
   private readonly bool[] _allowed;
   private readonly WireFallback _fallback;

   public AsciiSubsetWireEncoder(string alphabet, WireFallback fallback)
   {
      ArgumentNullException.ThrowIfNull(fallback);

      _allowed = CreateAllowedTable(alphabet);
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

         if (ch <= 0xFF && _allowed[ch])
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

   public bool TryFallback(char ch, out byte b)
   {
      switch (_fallback.Mode)
      {
         case WireFallbackOp.Throw:
            throw new InvalidOperationException($"Character U+{(int)ch:X4} cannot be encoded in Youtf75.");

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

   internal static bool[] CreateAllowedTable(string alphabet)
   {
      ArgumentException.ThrowIfNullOrEmpty(alphabet);

      var allowed = new bool[0x100];
      
      foreach (char ch in alphabet)
      {
         if (ch >= 0x100)
         {
            throw new ArgumentOutOfRangeException(
               nameof(alphabet), $"Alphabet contains non-ASCII character U+{(int)ch:X4}"
            );
         }
         allowed[ch] = true;
      }
      
      return allowed;
   }
}
