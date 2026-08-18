namespace Brx.AI.Tokenizer.Wire;

public static class HuggingFaceByteMapping
{
   // ---------------------------------------------------------------
   // Single source of truth: HF identity bytes
   // ---------------------------------------------------------------
   public static bool IsIdentityByte(int b)
   {
      return b >= 33 && b <= 126 ||   // ASCII printable
             b >= 161 && b <= 172 ||  // Latin-1 printable
             b >= 174 && b <= 255;    // Latin-1 printable
   }

   // ---------------------------------------------------------------
   // Internal storage
   // ---------------------------------------------------------------
   private static readonly char[] _forward = new char[256];
   private static readonly byte[] _reverse = new byte[512];

   // ---------------------------------------------------------------
   // Public API: ReadOnlySpan views
   // ---------------------------------------------------------------
   public static ReadOnlySpan<char> Forward => _forward;
   public static ReadOnlySpan<byte> Reverse => _reverse;

   // ---------------------------------------------------------------
   // Static constructor builds everything once
   // ---------------------------------------------------------------
   static HuggingFaceByteMapping()
   {
      // rank[b] = index in nonPrintable[] or -1 if identity
      var rank = new int[256];
      var nonPrintable = new byte[256];
      int npCount = 0;

      // Build rank table and nonPrintable list
      for (int b = 0; b < 256; b++)
      {
         if (IsIdentityByte(b))
         {
            rank[b] = -1;
         }
         else
         {
            rank[b] = npCount;
            nonPrintable[npCount++] = (byte)b;
         }
      }

      // Forward mapping: byte -> unicode char
      for (int b = 0; b < 256; b++)
      {
         int r = rank[b];
         if (r < 0)
         {
            _forward[b] = (char)b;            // identity
         }
         else
         {
            _forward[b] = (char)(256 + r);    // non-printable -> U+0100 + rank
         }
      }

      // Reverse mapping: unicode codepoint -> byte
      // identity bytes
      for (int b = 0; b < 256; b++)
      {
         if (rank[b] < 0)
            _reverse[b] = (byte)b;
      }

      // non-printable bytes
      for (int i = 0; i < npCount; i++)
      {
         _reverse[256 + i] = nonPrintable[i];
      }
   }
}
