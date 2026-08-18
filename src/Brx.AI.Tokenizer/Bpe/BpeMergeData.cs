namespace Brx.AI.Tokenizer.Bpe;

public sealed class BpeMergeData
{
   public readonly List<BpeMergeRule> Rules;

   public BpeMergeData(List<BpeMergeRule> rules)
   {
      Rules = rules;
   }
}
