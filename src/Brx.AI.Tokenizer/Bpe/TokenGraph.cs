using System.Diagnostics.CodeAnalysis;

namespace Brx.AI.Tokenizer.Bpe;

public sealed class TokenGraph
{
   /// <summary>
   /// The root node index of the DAG. Always 0.
   /// </summary>
   public int RootNodeIndex => 0;

   /// <summary>
   /// Direct access to the root node of the DAG.
   /// </summary>
   public TokenGraphNode RootNode => Nodes[0];

   public TokenGraphNode[] Nodes { get; }
   public int[] TransitionTokenIds { get; }
   public int[] TransitionNodeIndices { get; }

   public TokenGraph(
       TokenGraphNode[] nodes,
       int[] transitionTokenIds,
       int[] transitionNodeIndices)
   {
      ArgumentNullException.ThrowIfNull(nodes);
      ArgumentNullException.ThrowIfNull(transitionTokenIds);
      ArgumentNullException.ThrowIfNull(transitionNodeIndices);

      Nodes = nodes;
      TransitionTokenIds = transitionTokenIds;
      TransitionNodeIndices = transitionNodeIndices;
   }

   /// <summary>
   /// Attempts to follow a transition from the given node using the specified tokenId.
   /// Returns true if a matching edge exists, and outputs the next node.
   /// </summary>
   public bool TryGetTransition(
       TokenGraphNode node,
       int tokenId,
       [NotNullWhen(true)] out TokenGraphNode? next)
   {
      int start = node.StartIndex;
      int end = node.EndIndex;
      int count = end - start;

      // Linear search for small ranges
      if (count <= 8)
      {
         for (int i = start; i < end; i++)
         {
            if (TransitionTokenIds[i] == tokenId)
            {
               next = Nodes[TransitionNodeIndices[i]];
               return true;
            }
         }

         next = default;
         return false;
      }

      // Binary search for larger ranges
      int lo = start;
      int hi = end - 1;

      while (lo <= hi)
      {
         int mid = (lo + hi) >> 1;
         int k = TransitionTokenIds[mid];

         if (tokenId == k)
         {
            next = Nodes[TransitionNodeIndices[mid]];
            return true;
         }

         if (tokenId < k)
            hi = mid - 1;
         else
            lo = mid + 1;
      }

      next = default;
      return false;
   }
}
