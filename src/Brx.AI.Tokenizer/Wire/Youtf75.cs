using System.Runtime.CompilerServices;

namespace Brx.Text;

internal static class Youtf75
{
   // UTF-7 Base64 alphabet
   private static readonly char[] B64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789*/".ToCharArray();

   public const char HIGH_SURROGATE_START = '\uD800';
   public const char HIGH_SURROGATE_END = '\uDBFF';
   public const char LOW_SURROGATE_START = '\uDC00';
   public const char LOW_SURROGATE_END = '\uDFFF';

   // ---------------------------------------------------------
   // Precomputed ASCII classification table
   // ---------------------------------------------------------

   // 0–127: true = ASCII safe non-syntax (part of 69-char alphabet)
   private static readonly int AsciiSafeNonSyntaxTableLength = 128;
   private static readonly bool[] AsciiSafeNonSyntaxTable = CreateAsciiSafeNonSyntaxTable();

   private static bool[] CreateAsciiSafeNonSyntaxTable()
   {
      var table = new bool[AsciiSafeNonSyntaxTableLength];

      // printable ASCII 0x20–0x7E
      for (int i = 0x20; i <= 0x7E; i++)
         table[i] = true;

      // control chars that are part of the 69-char alphabet: \n, \r, \t
      table['\n'] = true;
      table['\r'] = true;
      table['\t'] = true;

      // syntax chars must NOT be treated as safe non-syntax
      table['^'] = false;
      table['~'] = false;

      return table;
   }

   public static bool IsAsciiSafeNonSyntax(char ch)
   {
      return (ch < AsciiSafeNonSyntaxTableLength) && AsciiSafeNonSyntaxTable[ch];
   }

   public static bool IsAsciiLowerCase(char ch)
   {
      return (uint)(ch - 'a') <= 25; // ch >= 'a' && ch <= 'z' 
   }

   public static bool IsBmpUpperCase(char c)
   {
      var info = UnicodeCapsMapping.Get(c);
      return info.HasMapping && info.Upper == c;
   }

   public static bool IsMappableUpperCase(char high, char low)
   {
      return UnicodeCapsMapping.Get(high, low).IsMappableUpper;
   }

   public static bool IsPlusOrMinus(char ch)
   {
      return (uint)(ch - '+') <= 2 && ch != ','; // ch == '+' || ch == '-'
   }

   public static bool IsSurrogate(char ch)
   {
      return (ch & 0xF800) == 0xD800;
   }

   public static bool TryEmitLowerCaseUtf7(char ch, Span<char> output, ref int outputIndex)
   {
      // UTF-7 bit packing for 16-bit surrogate code unit
      byte hi = (byte)(ch >> 8);
      byte lo = (byte)(ch & 0xFF);

      int v0 = (hi >> 2) & 0x3F;
      int v1 = ((hi & 0x03) << 4) | ((lo >> 4) & 0x0F);
      int v2 = (lo & 0x0F) << 2;

      char c0 = B64[v0];
      char c1 = B64[v1];
      char c2 = B64[v2];

      int l0 = LowerCaseUtf7Length(c0);
      int l1 = LowerCaseUtf7Length(c1);
      int l2 = LowerCaseUtf7Length(c2);

      int needed = 2 + l0 + l1 + l2;

      if (outputIndex + needed > output.Length)
         return false;

      int plusMinusXorMask = GetPlusMinusXorMask(ch);

      output[outputIndex++] = (char)('+' ^ plusMinusXorMask);

      EmitLowercaseUtf7Char(c0, l0 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c1, l1 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c2, l2 > 1, output, ref outputIndex);

      output[outputIndex++] = (char)('-' ^ plusMinusXorMask);

      return true;
   }

   public static bool TryEmitLowerCaseUtf7(char high, char low, Span<char> output, ref int outputIndex)
   {
      // UTF-16BE bytes
      byte b0 = (byte)(high >> 8);
      byte b1 = (byte)(high & 0xFF);
      byte b2 = (byte)(low >> 8);
      byte b3 = (byte)(low & 0xFF);

      // 6 sextets
      int v0 = (b0 >> 2) & 0x3F;
      int v1 = ((b0 & 0x03) << 4) | ((b1 >> 4) & 0x0F);
      int v2 = ((b1 & 0x0F) << 2) | ((b2 >> 6) & 0x03);
      int v3 = b2 & 0x3F;
      int v4 = (b3 >> 2) & 0x3F;
      int v5 = (b3 & 0x03) << 4;

      // base64 chars
      char c0 = B64[v0];
      char c1 = B64[v1];
      char c2 = B64[v2];
      char c3 = B64[v3];
      char c4 = B64[v4];
      char c5 = B64[v5];

      // lowercase lengths (NO DUPLICATION)
      int l0 = LowerCaseUtf7Length(c0);
      int l1 = LowerCaseUtf7Length(c1);
      int l2 = LowerCaseUtf7Length(c2);
      int l3 = LowerCaseUtf7Length(c3);
      int l4 = LowerCaseUtf7Length(c4);
      int l5 = LowerCaseUtf7Length(c5);

      int needed = 2 + l0 + l1 + l2 + l3 + l4 + l5;
      if (outputIndex + needed > output.Length)
         return false;

      int plusMinusXorMask = GetPlusMinusXorMask(high, low);

      output[outputIndex++] = (char)('+' ^ plusMinusXorMask);

      EmitLowercaseUtf7Char(c0, l0 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c1, l1 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c2, l2 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c3, l3 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c4, l4 > 1, output, ref outputIndex);
      EmitLowercaseUtf7Char(c5, l5 > 1, output, ref outputIndex);

      output[outputIndex++] = (char)('-' ^ plusMinusXorMask);
      
      return true;
   }

   private static int GetPlusMinusXorMask(char ch)
   {
      // if ch is WordBoundary
      //   then we return 6, because '+' xor 6 == '-' and  '-' xor 6 == '+'
      // otherwise
      //   we return 0, because '+' xor 0 == '+' and  '-' xor 0 == '-'

      bool wb = WordBoundary.IsWordBoundary(ch);
      return Unsafe.As<bool, byte>(ref wb) * 6; 
   }

   private static int GetPlusMinusXorMask(char high, char low)
   {
      // if surrogate(high,low) is WordBoundary
      //   then we return 6, because '+' xor 6 == '-' and  '-' xor 6 == '+'
      // otherwise
      //   we return 0, because '+' xor 0 == '+' and  '-' xor 0 == '-'

      bool wb = WordBoundary.IsWordBoundary(high, low);
      return Unsafe.As<bool, byte>(ref wb) * 6;
   }

   private static int LowerCaseUtf7Length(char ch)
   {
      // instead of the naive
      //
      // return ('a' <= ch) && (ch <= 'z') ? 2 : 1;
      //
      // there is a branchless version:
      //
      uint x = (uint)(ch - 'a');
      uint y = (uint)(25 - x);
      uint isLower = (x ^ y) >> 31;   // 1 if in range, 0 otherwise
      return 1 + (int)isLower;

   }

   private static void EmitLowercaseUtf7Char(char ch, bool isLower, Span<char> output, ref int outputIndex)
   {
      if (isLower) 
      {
         // 'a'..'z' -> " a".." z"
         output[outputIndex++] = ' ';
         output[outputIndex++] = ch;
      }
      else if (ch >= 'A' && ch <= 'Z')
      {
         // 'A' – 'Z' -> 'a' – 'z'
         output[outputIndex++] = (char)(ch - 'A' + 'a');
      }
      else
      {
         // anything else unchanged
         output[outputIndex++] = ch;
      }
   }
}
