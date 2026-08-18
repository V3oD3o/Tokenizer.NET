using Brx.AI.Tokenizer.Dsl;

namespace Brx.AI.Tokenizer.Wire;

public readonly struct WireFallbackInstruction
{
   public readonly WireFallbackOp Op;
   public readonly int Arg; // only used if Op == Replace

   public WireFallbackInstruction(WireFallbackOp op, int arg)
   {
      Op = op;
      Arg = arg;
   }

   public static WireFallbackInstruction FromMiniDsl(MiniDslInstruction mini)
   {
      WireFallbackOp op = mini.OpName.ToLowerInvariant() switch
      {
         "throw" => WireFallbackOp.Throw,
         "skip" => WireFallbackOp.Skip,
         "replace" => WireFallbackOp.Replace,
         _ => throw new FormatException("Unknown fallback op: " + mini.OpName)
      };

      int arg = (mini.ArgLiteral is null)
          ? -1
          : MiniDslParser.ParseNumericLiteral(mini.ArgLiteral);

      return new WireFallbackInstruction(op, arg);
   }
}
