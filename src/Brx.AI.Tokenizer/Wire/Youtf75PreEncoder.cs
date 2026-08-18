using Brx.Text;

namespace Brx.Hpc.Blocks;

public sealed class Youtf75PreEncoder : IPassThroughBlockConverter<char>
{
   // Only a high surrogate can be pending across block boundaries.
   private char _pendingHighSurrogate = '\0';

   public bool CanPassThrough(ReadOnlySpan<char> input, int inputIndex)
   {
      // Pass-through is only allowed when there is no pending high surrogate
      if (_pendingHighSurrogate != '\0')
         return false;

      // Scan the block for any character that requires conversion
      int inputLength = input.Length;
      for (int i = inputIndex; i < inputLength; i++)
      {
         char c = input[i];

         // Any surrogate (high or low) disables pass-through
         if (Youtf75.IsSurrogate(c))
            return false;

         // '+' (and '-') disables pass-through because we want to escape them in the pre-encoder,
         // to avoid more convoluted logic in the second pass. (Lone surrogates are encoded in the
         // pre-encoder phase as +...- escape sequences and we need to leave those intact in the
         // second pass)
         if (Youtf75.IsPlusOrMinus(c))
            return false;
      }

      // No surrogate, no '+', no pending state -> safe to pass through
      return true;
   }

   public void Convert(
       ReadOnlySpan<char> input,
       ref int inputIndex,
       Span<char> output,
       ref int outputIndex,
       out bool completed
   )
   {
      completed = true;

      int inputLength = input.Length;
      int outputLength = output.Length;

      while (inputIndex < inputLength)
      {
         // If we have no pending high surrogate, load one and continue
         if (_pendingHighSurrogate == '\0')
         {
            char c = input[inputIndex];

            if (Youtf75.IsSurrogate(c))
            {
               // If high surrogate -> store as pending and continue
               if (c <= Youtf75.HIGH_SURROGATE_END) // char.IsHighSurrogate(c)
               {
                  _pendingHighSurrogate = c;
                  inputIndex++;
                  continue;
               }
               // If low surrogate -> lone
               else
               {
                  if (!Youtf75.TryEmitLowerCaseUtf7(c, output, ref outputIndex))
                  {
                     completed = false;
                     return;
                  }

                  inputIndex++;
                  continue;
               }
            }

            // '+' -> "+-", '-' -> "-+"
            if (Youtf75.IsPlusOrMinus(c))
            {
               if (outputIndex + 2 > outputLength)
               {
                  completed = false;
                  return;
               }

               output[outputIndex++] = c;
               output[outputIndex++] = (char)(c ^ 6); // because '+' xor 6 == '-' and  '-' xor 6 == '+' 
               inputIndex++;
               continue;
            }

            // Normal char
            if (outputIndex >= outputLength)
            {
               completed = false;
               return;
            }

            output[outputIndex++] = c;
            inputIndex++;
            continue;
         }

         // We have a pending high surrogate -> resolve it here
         char high = _pendingHighSurrogate;

         // If no more input -> leave pending unresolved
         if (inputIndex >= inputLength)
            return;

         char low = input[inputIndex];

         if (char.IsLowSurrogate(low))
         {
            // Full pair
            if (outputIndex + 2 > outputLength)
            {
               completed = false;
               return;
            }

            if(Youtf75.IsMappableUpperCase(high, low))
            {
               output[outputIndex++] = low;
               output[outputIndex++] = high;
            }
            else
            {
               output[outputIndex++] = high;
               output[outputIndex++] = low;
            }

            inputIndex++;
            _pendingHighSurrogate = '\0';
            continue;
         }
         else
         {
            // Lone high surrogate
            if (!Youtf75.TryEmitLowerCaseUtf7(high, output, ref outputIndex))
            {
               completed = false;
               return;
            }

            _pendingHighSurrogate = '\0';
            continue;
         }
      }
   }

   public void Flush(Span<char> output, ref int outputIndex, out bool completed)
   {
      completed = true;

      // Pending high surrogate at flush -> definitely lone
      if (_pendingHighSurrogate != '\0')
      {
         if (!Youtf75.TryEmitLowerCaseUtf7(_pendingHighSurrogate, output, ref outputIndex))
         {
            completed = false;
            return;
         }

         _pendingHighSurrogate = '\0';
      }
   }
}
