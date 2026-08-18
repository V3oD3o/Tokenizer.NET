using Brx.Unsorted;
using System.Globalization;

namespace Brx.Text
{
   public static class WordBoundary
   {
      private static readonly EnumDictionary<UnicodeCategory, bool> _wb =
          new EnumDictionary<UnicodeCategory, bool>(cat =>
              cat switch
              {
                 UnicodeCategory.LowercaseLetter => false,
                 UnicodeCategory.UppercaseLetter => false,
                 UnicodeCategory.TitlecaseLetter => false,
                 UnicodeCategory.ModifierLetter => false,
                 UnicodeCategory.OtherLetter => false,

                 UnicodeCategory.DecimalDigitNumber => false,
                 UnicodeCategory.LetterNumber => false,
                 UnicodeCategory.OtherNumber => false,

                 UnicodeCategory.NonSpacingMark => false,
                 UnicodeCategory.SpacingCombiningMark => false,
                 UnicodeCategory.EnclosingMark => false,

                 _ => true
              });
      public static bool IsWordBoundary(char c)
      {
         // Lone surrogates are always boundary
         if ((c & 0xF800) == 0xD800)
            return true;

         UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
         return _wb[cat];
      }
      public static bool IsWordBoundary(char high, char low)
      {
         if (!char.IsHighSurrogate(high))
            throw new ArgumentException("High surrogate expected.", nameof(high));

         if (!char.IsLowSurrogate(low))
            throw new ArgumentException("Low surrogate expected.", nameof(low));

         int codePoint = 0x10000 + ((high - 0xD800) << 10) + (low - 0xDC00);

         UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(codePoint);
         return _wb[cat];
      }
   }
}