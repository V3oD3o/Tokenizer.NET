namespace Brx.AI.Tokenizer.Bpe;

public static class BpeMergeLoader
{
   public static BpeMergeData Load(TextReader reader, BpeVocab vocab)
   {
      var rules = new List<BpeMergeRule>();
      string? line;
      int lineNum = 0;
      while ((line = reader.ReadLine()) is not null)
      {
         lineNum++;
         line = line.Trim();
         if (line.Length == 0 || line[0] == '#')
            continue;

         var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
         if (parts.Length != 2)
            throw new InvalidOperationException($"Unexpected whitespace in merge list at line {lineNum}.");

         if (!vocab.TryGetId(parts[0], out int leftId))
            throw new InvalidOperationException($"Merged token '{parts[0]}' not found in vocab.");

         if (!vocab.TryGetId(parts[1], out int rightId))
            throw new InvalidOperationException($"Merged token '{parts[1]}' not found in vocab.");

         string mergedToken = parts[0] + parts[1];
         if (!vocab.TryGetId(mergedToken, out int mergedId))
            throw new InvalidOperationException($"Merged token '{mergedToken}' not found in vocab.");

         rules.Add(new BpeMergeRule(lineNum, leftId, rightId, mergedId));
      }

      return new BpeMergeData(rules);
   }
}
