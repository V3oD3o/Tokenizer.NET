namespace Brx.AI.Tokenizer.Wire;

public sealed class WireFallback
{
   public static readonly WireFallback Throw = new WireFallback(WireFallbackOp.Throw, '\0');

   public static readonly WireFallback Skip = new WireFallback(WireFallbackOp.Skip, '\0');

   public static readonly WireFallback QuestionMark = new WireFallback(WireFallbackOp.Replace, '?');

   public static readonly WireFallback UnicodeReplacement = new WireFallback(WireFallbackOp.Replace, '\uFFFD');

   public readonly WireFallbackOp Mode;
   public readonly char Replacement;

   public WireFallback(WireFallbackOp mode, char replacement)
   {
      Mode = mode;
      Replacement = replacement;
   }

   public bool TryMap(byte b, out char ch)
   {
      switch (Mode)
      {
         case WireFallbackOp.Throw:
            throw InvalidByteException(b);

         case WireFallbackOp.Skip:
            ch = '\0';
            return false;

         case WireFallbackOp.Replace:
            ch = Replacement;
            return true;
      }
      throw new InvalidOperationException("Unknown fallback mode: " + Mode);
   }

   public bool TryFallback(char ch, out byte b)
   {
      switch (Mode)
      {
         case WireFallbackOp.Throw:
            throw new InvalidOperationException($"Character U+{(int)ch:X4} cannot be encoded in Youtf75.");

         case WireFallbackOp.Skip:
            b = 0;
            return false;

         case WireFallbackOp.Replace:
            b = (byte)Replacement;
            return true;
      }

      b = 0;
      return false;
   }
   
   public static Exception InvalidByteException(byte b)
   {
      return new InvalidOperationException($"Invalid byte 0x{b:X2}");
   }
}
