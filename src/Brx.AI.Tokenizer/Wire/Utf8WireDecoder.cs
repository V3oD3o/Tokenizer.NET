using System.Text;
using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public sealed class Utf8WireDecoder : IBlockConverter<byte, char>
{
   private readonly Decoder _decoder;

   public Utf8WireDecoder()
   {
      // The built-in UTF-8 decoder handles partial sequences and fallback.
      _decoder = Encoding.UTF8.GetDecoder();
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

      while (inputIndex < inputLength)
      {
         // Convert as many bytes as possible into chars.
         int bytesUsed;
         int charsUsed;
         bool decodingCompleted;

         _decoder.Convert(
             input.Slice(inputIndex),
             output.Slice(outputIndex),
             flush: false,
             out bytesUsed,
             out charsUsed,
             out decodingCompleted);

         inputIndex += bytesUsed;
         outputIndex += charsUsed;

         // If the decoder reports incomplete processing, the caller must provide more output space.
         if (!decodingCompleted)
         {
            completed = false;
            return;
         }

         // If no progress was made, the output buffer is full.
         if (bytesUsed == 0 && charsUsed == 0)
         {
            completed = false;
            return;
         }
      }
   }

   public void Flush(
       Span<char> output,
       ref int outputIndex,
       out bool completed)
   {
      // Flush any remaining buffered UTF-8 state.
      int bytesUsed;
      int charsUsed;
      bool decodingCompleted;

      _decoder.Convert(
          ReadOnlySpan<byte>.Empty,
          output.Slice(outputIndex),
          flush: true,
          out bytesUsed,
          out charsUsed,
          out decodingCompleted);

      outputIndex += charsUsed;
      completed = decodingCompleted;
   }
}
