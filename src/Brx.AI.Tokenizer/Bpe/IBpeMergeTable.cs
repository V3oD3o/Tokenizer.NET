namespace Brx.AI.Tokenizer.Bpe;

public interface IBpeMergeTable
{
   bool TryGetMergedId(int leftId, int rightId, out int mergedId, out int priority);
   bool TryGetMergedId(string left, string right, out int mergedId, out int priority);
}
