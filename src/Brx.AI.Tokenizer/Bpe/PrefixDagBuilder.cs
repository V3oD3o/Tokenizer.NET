namespace Brx.AI.Tokenizer.Bpe;

public sealed class PrefixDagBuilder
{
   private readonly struct EdgeKey : IEquatable<EdgeKey>
   {
      public readonly int NodeId;
      public readonly int TokenId;
      private readonly int _hashCode;

      public EdgeKey(int nodeId, int tokenId)
      {
         NodeId = nodeId;
         TokenId = tokenId;
         _hashCode = nodeId * 397 ^ tokenId;
      }

      public bool Equals(EdgeKey other)
          => NodeId == other.NodeId && TokenId == other.TokenId;

      public override bool Equals(object? obj)
          => obj is EdgeKey other && Equals(other);

      public override int GetHashCode()
          => _hashCode;
   }

   private readonly List<TokenGraphNode> _nodes;
   private readonly List<int> _transitionTokenIds;
   private readonly List<int> _transitionNodeIndices;

   private readonly Dictionary<EdgeKey, int> _edges;

   public PrefixDagBuilder()
   {
      _nodes = new List<TokenGraphNode>();
      _transitionTokenIds = new List<int>();
      _transitionNodeIndices = new List<int>();
      _edges = new Dictionary<EdgeKey, int>();

      // Root node
      _nodes.Add(new TokenGraphNode
      {
         NodeIndex = 0,
         MergedTokenId = -1,
         StartIndex = 0,
         EndIndex = 0
      });
   }

   // ---------------------------------------------------------
   // UPDATED: IEnumerable<int> instead of ReadOnlySpan<int>
   // ---------------------------------------------------------
   public int InsertToken(IEnumerable<int> tokenIds, int mergedTokenId)
   {
      int current = 0;

      foreach (int tid in tokenIds)
      {
         var key = new EdgeKey(current, tid);

         if (!_edges.TryGetValue(key, out int next))
         {
            next = _nodes.Count;

            _nodes.Add(new TokenGraphNode
            {
               NodeIndex = next,
               MergedTokenId = -1,
               StartIndex = 0,
               EndIndex = 0
            });

            if (!_edges.TryAdd(key, next))
               throw new InvalidOperationException(
                   $"Duplicate edge detected for node {current} and tokenId {tid}.");
         }

         current = next;
      }

      // Mark leaf node
      _nodes[current].MergedTokenId = mergedTokenId;
      return current;
   }

   public TokenGraph Build()
   {
      var grps = _edges
          .GroupBy(edge => edge.Key.NodeId)
          .OrderBy(grp => grp.Key);

      int offset = 0;

      foreach (var grp in grps)
      {
         int nodeId = grp.Key;
         var node = _nodes[nodeId];

         node.StartIndex = offset;

         foreach (var edge in grp.OrderBy(edge => edge.Key.TokenId))
         {
            int tid = edge.Key.TokenId;
            int nextNodeId = edge.Value;

            _transitionTokenIds.Add(tid);
            _transitionNodeIndices.Add(nextNodeId);

            offset++;
         }

         node.EndIndex = offset;
      }

      return new TokenGraph(
          _nodes.ToArray(),
          _transitionTokenIds.ToArray(),
          _transitionNodeIndices.ToArray()
      );
   }

   public static TokenGraph BuildPrefixDag(BpeVocab vocab)
   {
      ArgumentNullException.ThrowIfNull(vocab);

      var builder = new PrefixDagBuilder();

      for (int tokenId = 0; tokenId < vocab.IdToToken.Count; tokenId++)
      {
         string tokenString = vocab.IdToToken[tokenId];


         builder.InsertToken(GetTokenIds(vocab, tokenString), tokenId);
      }

      return builder.Build();
   }

   // Iterator function -> no stackalloc, no arrays
   private static IEnumerable<int> GetTokenIds(BpeVocab vocab, string token)
   {
      for (int i = 0; i < token.Length; i++)
         yield return vocab.GetCharTokenId(token[i]);
   }
}
