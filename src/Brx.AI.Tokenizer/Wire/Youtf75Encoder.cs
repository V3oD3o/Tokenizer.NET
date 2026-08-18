using System;
using Brx.Text;

namespace Brx.Hpc.Blocks;

public sealed class Youtf75Encoder : IBlockConverter<char, char>
{
   private bool _inCaps;
   private int _capsCount;

   // CAPS-run first element (BMP or surrogate low–high)
   private char _first0;
   private char _first1;

   public void Convert(
      ReadOnlySpan<char> input,
      ref int inputIndex,
      Span<char> output,
      ref int outputIndex,
      out bool completed
   )
   {
      completed = false;

      int inputLength = input.Length;
      int outputLength = output.Length;

      while (inputIndex < inputLength)
      {
         char c = input[inputIndex];

         if (Youtf75.IsSurrogate(c)) 
         {
            //
            // HIGH surrogate -> non-mappable surrogate pair -> UTF-7
            //
            if (c <= Youtf75.HIGH_SURROGATE_END) // char.IsHighSurrogate(c)
            {
               if (_inCaps)
               {
                  if (!FlushCapsRun(output, ref outputIndex))
                     return;

                  _inCaps = false;
                  _capsCount = 0;
               }

               int inputIndexPlusOne = inputIndex + 1;
               if (inputIndexPlusOne >= inputLength)
                  return;

               char low = input[inputIndexPlusOne];

               if (!Youtf75.TryEmitLowerCaseUtf7(c, low, output, ref outputIndex))
                  return;

               inputIndex += 2;
               continue;
            }

            //
            // LOW surrogate -> mappable uppercase surrogate scalar (low–high)
            // PASS1 guarantees: every mappable pair arrives as low–high
            //
            else // char.IsLowSurrogate(c)
            {
               int inputIndexPlusOne = inputIndex + 1;
               if (inputIndexPlusOne >= input.Length)
                  return;

               char high = input[inputIndexPlusOne];

               // Always mappable uppercase surrogate scalar (low–high)

               if (!_inCaps)
               {
                  StartCapsRunSurrogate(c, high);
               }
               else
               {
                  _capsCount++;

                  if (_capsCount == 2)
                  {
                     if (!EmitCapsHeader(output, ref outputIndex))
                        return;

                     if (!EmitPayloadFirst(output, ref outputIndex))
                        return;
                  }

                  if (!EmitPayloadSurrogate(c, high, output, ref outputIndex))
                     return;
               }

               inputIndex += 2;
               continue;
            }
         }


         // ---------------------------------------------------------
         // BMP uppercase -> CAPS-run elem
         // ---------------------------------------------------------
         if (Youtf75.IsBmpUpperCase(c))
         {
            if (!_inCaps)
            {
               StartCapsRunBmp(c);
            }
            else
            {
               _capsCount++;

               if (_capsCount == 2)
               {
                  if (!EmitCapsHeader(output, ref outputIndex))
                     return;

                  if (!EmitPayloadFirst(output, ref outputIndex))
                     return;
               }

               if (!EmitPayloadBmp(c, output, ref outputIndex))
                  return;
            }

            inputIndex++;
            continue;
         }

         // ---------------------------------------------------------
         // Non-uppercase -> close CAPS-run if active
         // ---------------------------------------------------------
         if (_inCaps)
         {
            if (!FlushCapsRun(output, ref outputIndex))
               return;

            _inCaps = false;
            _capsCount = 0;
         }

         // ---------------------------------------------------------
         // ASCII lowercase
         // ---------------------------------------------------------
         if (Youtf75.IsAsciiLowerCase(c))
         {
            if (outputIndex >= outputLength)
               return;

            output[outputIndex++] = c;
            inputIndex++;
            continue;
         }

         // ---------------------------------------------------------
         // ASCII safe non-syntax
         // ---------------------------------------------------------
         if (Youtf75.IsAsciiSafeNonSyntax(c))
         {
            if (outputIndex >= outputLength)
               return;

            output[outputIndex++] = c;
            inputIndex++;
            continue;
         }

         // ---------------------------------------------------------
         // Syntax escapes
         // ---------------------------------------------------------
         if (c == '~')
         {
            if (outputIndex + 2 > outputLength)
               return;

            output[outputIndex++] = '~';
            output[outputIndex++] = '~';
            inputIndex++;
            continue;
         }

         if (c == '^')
         {
            if (outputIndex + 2 > outputLength)
               return;

            output[outputIndex++] = '~';
            output[outputIndex++] = '^';
            inputIndex++;
            continue;
         }

         // ---------------------------------------------------------
         // BMP non-ASCII -> UTF-7
         // ---------------------------------------------------------
         if (!Youtf75.TryEmitLowerCaseUtf7(c, output, ref outputIndex))
            return;

         inputIndex++;
      }

      completed = true;
   }

   public void Flush(Span<char> output, ref int outputIndex, out bool completed)
   {
      if (_inCaps)
      {
         if (!FlushCapsRun(output, ref outputIndex))
         {
            completed = false;
            return;
         }

         _inCaps = false;
         _capsCount = 0;
      }

      completed = true;
   }

   // ---------------------------------------------------------
   // CAPS-run helpers
   // ---------------------------------------------------------

   private void StartCapsRunBmp(char upper)
   {
      _inCaps = true;
      _capsCount = 1;
      _first0 = upper;
      _first1 = '\0';
   }

   private void StartCapsRunSurrogate(char low, char high)
   {
      _inCaps = true;
      _capsCount = 1;
      _first0 = low;
      _first1 = high;
   }

   private bool EmitCapsHeader(Span<char> output, ref int outputIndex)
   {
      if (outputIndex + 2 > output.Length)
         return false;

      output[outputIndex++] = '^';
      output[outputIndex++] = '^';
      return true;
   }

   private bool FlushCapsRun(Span<char> output, ref int outputIndex)
   {
      if (_capsCount == 1)
      {
         if (outputIndex >= output.Length)
            return false;

         output[outputIndex++] = '^';
         if (!EmitPayloadFirst(output, ref outputIndex))
         {
            outputIndex--;
            return false;
         }
      }
      else
      {
         if (outputIndex >= output.Length)
            return false;

         output[outputIndex++] = '^';
      }

      return true;
   }

   private bool EmitPayloadFirst(Span<char> output, ref int outputIndex)
   {
      return Youtf75.IsSurrogate(_first0)
         ? EmitPayloadSurrogate(_first0, _first1, output, ref outputIndex)
         : EmitPayloadBmp(_first0, output, ref outputIndex);
   }

   private bool EmitPayloadBmp(char upper, Span<char> output, ref int outputIndex)
   {
      var info = UnicodeCapsMapping.Get(upper);
      char lower = info.Lower;

      if (Youtf75.IsAsciiLowerCase(lower))
      {
         if (outputIndex >= output.Length)
            return false;

         output[outputIndex++] = lower;
         return true;
      }

      return Youtf75.TryEmitLowerCaseUtf7(lower, output, ref outputIndex);
   }

   private bool EmitPayloadSurrogate(char low, char high, Span<char> output, ref int outputIndex)
   {
      var info = UnicodeCapsMapping.Get(high, low);
      var lp = info.Lower;
      return Youtf75.TryEmitLowerCaseUtf7(lp.High, lp.Low, output, ref outputIndex);
   }
}
