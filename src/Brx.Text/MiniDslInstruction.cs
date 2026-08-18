namespace Brx.AI.Tokenizer.Dsl;

public readonly struct MiniDslInstruction
{
   public readonly string OpName;
   public readonly string? ArgLiteral;

   public MiniDslInstruction(string opName, string? argLiteral)
   {
      OpName = opName;
      ArgLiteral = argLiteral;
   }
}
