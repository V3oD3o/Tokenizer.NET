using System.Collections.ObjectModel;

namespace Brx.AI.Tokenizer.Bpe;

public sealed class BpeMergeTable : IBpeMergeTable
{
   public readonly struct MergeEntry
   {
      public readonly int Merged;
      public readonly int Priority;

      public MergeEntry(int merged, int priority)
      {
         Merged = merged;
         Priority = priority;
      }
   }

   private readonly ReadOnlyDictionary<BpePair, MergeEntry> _pairToEntry;
   private readonly BpeVocab _vocab;

   public IReadOnlyDictionary<BpePair, MergeEntry> PairToMerged => _pairToEntry;

   private BpeMergeTable(ReadOnlyDictionary<BpePair, MergeEntry> dict, BpeVocab vocab)
   {
      _pairToEntry = dict;
      _vocab = vocab;
   }

   public static BpeMergeTable Build(BpeMergeData data, BpeVocab vocab)
   {
      var dict = new Dictionary<BpePair, MergeEntry>(data.Rules.Count);

      foreach (var rule in data.Rules)
      {
         var pair = new BpePair(rule.Left, rule.Right);

         if (!dict.TryAdd(pair, new MergeEntry(rule.Merged, rule.LineNum)))
            throw new InvalidOperationException($"Duplicate merge rule for pair ({rule.Left},{rule.Right}).");
      }

      return new BpeMergeTable(new ReadOnlyDictionary<BpePair, MergeEntry>(dict), vocab);
   }

   public static BpeMergeTable Load(string mergesTxtPath, BpeVocab vocab)
   {
      using var reader = new StreamReader(mergesTxtPath);
      return Load(reader, vocab);
   }

   public static BpeMergeTable Load(TextReader reader, BpeVocab vocab)
   {
      var data = BpeMergeLoader.Load(reader, vocab);
      return Build(data, vocab);
   }

   public bool TryGetMergedId(int leftId, int rightId, out int mergedId)
   {
      if (_pairToEntry.TryGetValue(new BpePair(leftId, rightId), out var entry))
      {
         mergedId = entry.Merged;
         return true;
      }

      mergedId = default;
      return false;
   }

   public bool TryGetMergedId(int leftId, int rightId, out int mergedId, out int priority)
   {
      if (_pairToEntry.TryGetValue(new BpePair(leftId, rightId), out var entry))
      {
         mergedId = entry.Merged;
         priority = entry.Priority;
         return true;
      }

      mergedId = default;
      priority = int.MaxValue;
      return false;
   }

   public bool TryGetMergedId(string left, string right, out int mergedId)
   {
      if (!_vocab.TryGetId(left, out int leftId) ||
          !_vocab.TryGetId(right, out int rightId))
      {
         mergedId = default;
         return false;
      }

      return TryGetMergedId(leftId, rightId, out mergedId);
   }

   public bool TryGetMergedId(string left, string right, out int mergedId, out int priority)
   {
      if (!_vocab.TryGetId(left, out int leftId) ||
          !_vocab.TryGetId(right, out int rightId))
      {
         mergedId = default;
         priority = int.MaxValue;
         return false;
      }

      return TryGetMergedId(leftId, rightId, out mergedId, out priority);
   }
}
