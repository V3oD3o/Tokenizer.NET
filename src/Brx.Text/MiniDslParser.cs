using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Brx.AI.Tokenizer.Dsl
{
   public static class MiniDslParser
   {
      private static readonly Regex CommandRegex = new Regex(
         @"^(?<op>[A-Za-z][A-Za-z0-9_]*)(?:\((?<arg>'(?:\\.|[^\\'])*'|[^()]*)\))?$", RegexOptions.Compiled
      );

      private static readonly int OpGroup = CommandRegex.GroupNumberFromName("op");

      private static readonly int ArgGroup = CommandRegex.GroupNumberFromName("arg");

      public static MiniDslInstruction ParseInstruction(string command)
      {
         command = command.Trim();

         var m = CommandRegex.Match(command);
         if (!m.Success)
            throw new FormatException("Invalid command syntax: " + command);

         string op = m.Groups[OpGroup].Value;
         string arg = m.Groups[ArgGroup].Value;

         return new MiniDslInstruction(
             op,
             string.IsNullOrEmpty(arg) ? null : arg.Trim()
         );
      }

      // ---------------------------------------------------------
      // Literal parsing
      // ---------------------------------------------------------

      private static readonly Regex CharLiteralRegex = new Regex(@"^'(?<char>\\.|.)'$", RegexOptions.Compiled);

      private static readonly int CharGroup = CharLiteralRegex.GroupNumberFromName("char");

      private static readonly Regex HexRegex = new Regex(@"^0x(?<hex>[0-9A-Fa-f]+)$", RegexOptions.Compiled);

      private static readonly int HexGroup = HexRegex.GroupNumberFromName("hex");

      private static readonly Regex DecRegex = new Regex(@"^(?<dec>[0-9]+)$", RegexOptions.Compiled);

      private static readonly int DecGroup = DecRegex.GroupNumberFromName("dec");

      /// <summary>
      /// Parses a literal into an integer value. Supports:
      /// - character literals: 'x', '\n', '\t', '\xFF', '\u00A0'
      /// - hex numeric: 0xFF
      /// - decimal numeric: 255
      /// </summary>
      public static int ParseNumericLiteral(string lit)
      {
         lit = lit.Trim();

         // Character literal
         var mChar = CharLiteralRegex.Match(lit);
         if (mChar.Success)
         {
            string token = mChar.Groups[CharGroup].Value;
            return ParseCharEscape(token);
         }

         // Hex numeric
         var mHex = HexRegex.Match(lit);
         if (mHex.Success)
            return Convert.ToInt32(mHex.Groups[HexGroup].Value, 16);

         // Decimal numeric
         var mDec = DecRegex.Match(lit);
         if (mDec.Success)
            return int.Parse(mDec.Groups[DecGroup].Value);

         throw new FormatException("Invalid literal: " + lit);
      }

      /// <summary>
      /// Parses escape sequences inside character literals.
      /// </summary>
      private static int ParseCharEscape(string token)
      {
         // Simple character
         if (token.Length == 1 && token[0] != '\\')
            return token[0];

         // Escape sequence
         if (token.StartsWith('\\'))
         {
            switch (token)
            {
               case "\\n": return '\n';
               case "\\r": return '\r';
               case "\\t": return '\t';
               case "\\\\": return '\\';
               case "\\'": return '\'';

               default:
                  if (token.StartsWith("\\x"))
                     return Convert.ToInt32(token.Substring(2), 16);

                  if (token.StartsWith("\\u"))
                     return Convert.ToInt32(token.Substring(2), 16);

                  throw new FormatException("Invalid escape sequence: " + token);
            }
         }

         throw new FormatException("Invalid character literal: " + token);
      }
   }
}

