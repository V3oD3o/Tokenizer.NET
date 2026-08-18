namespace Brx.AI.Tokenizer.Bpe;

public sealed class FlatBpeMergeTable : IBpeMergeTable
{
   private readonly int[] _leftIndex;
   private readonly int[] _leftCount;
   private readonly int[] _rightKeys;
   private readonly int[] _mergedValues;
   private readonly int[] _priorities;

   public FlatBpeMergeTable(
       int[] leftIndex,
       int[] leftCount,
       int[] rightKeys,
       int[] mergedValues,
       int[] priorities)
   {
      _leftIndex = leftIndex;
      _leftCount = leftCount;
      _rightKeys = rightKeys;
      _mergedValues = mergedValues;
      _priorities = priorities;
   }

   public bool TryGetMergedId(int leftId, int rightId, out int mergedId)
   {
      int start = _leftIndex[leftId];
      int count = _leftCount[leftId];

      for (int i = 0; i < count; i++)
      {
         if (_rightKeys[start + i] == rightId)
         {
            mergedId = _mergedValues[start + i];
            return true;
         }
      }

      mergedId = default;
      return false;
   }

   public bool TryGetMergedId(int leftId, int rightId, out int mergedId, out int priority)
   {
      int start = _leftIndex[leftId];
      int count = _leftCount[leftId];

      for (int i = 0; i < count; i++)
      {
         if (_rightKeys[start + i] == rightId)
         {
            mergedId = _mergedValues[start + i];
            priority = _priorities[start + i];
            return true;
         }
      }

      mergedId = default;
      priority = int.MaxValue;
      return false;
   }

   public bool TryGetMergedId(string left, string right, out int mergedId)
   {
      throw new NotImplementedException();
   }

   public bool TryGetMergedId(string left, string right, out int mergedId, out int priority)
   {
      throw new NotImplementedException();
   }

   public static FlatBpeMergeTable Load(string mergesTxtPath, BpeVocab vocab)
   {
      using var reader = new StreamReader(mergesTxtPath);
      return Load(reader, vocab);
   }

   public static FlatBpeMergeTable Load(TextReader reader, BpeVocab vocab)
   {
      var data = BpeMergeLoader.Load(reader, vocab);
      return Build(data, vocab);
   }

   public static FlatBpeMergeTable Build(BpeMergeData data, BpeVocab vocab)
   {
      int vocabSize = vocab.IdToToken.Count;
      var groups = new Dictionary<int, List<(int right, int merged, int priority)>>();

      foreach (var rule in data.Rules)
      {
         if (!groups.TryGetValue(rule.Left, out var list))
            groups[rule.Left] = list = new List<(int, int, int)>();

         list.Add((rule.Right, rule.Merged, rule.LineNum));
      }

      var leftIndex = new int[vocabSize];
      var leftCount = new int[vocabSize];

      int totalPairs = groups.Values.Sum(g => g.Count);

      var rightKeys = new int[totalPairs];
      var mergedValues = new int[totalPairs];
      var priorities = new int[totalPairs];

      int offset = 0;

      for (int left = 0; left < vocabSize; left++)
      {
         if (!groups.TryGetValue(left, out var list))
         {
            leftIndex[left] = offset;
            leftCount[left] = 0;
            continue;
         }

         leftIndex[left] = offset;
         leftCount[left] = list.Count;

         foreach (var (right, merged, priority) in list)
         {
            rightKeys[offset] = right;
            mergedValues[offset] = merged;
            priorities[offset] = priority;
            offset++;
         }
      }

      return new FlatBpeMergeTable(leftIndex, leftCount, rightKeys, mergedValues, priorities);
   }
}
