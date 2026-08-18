using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Bpe;

public sealed class PrefixDagWalker : IBlockConverter<int, int>
{
   private readonly TokenGraph _graph;
   private TokenGraphNode _node;

   public PrefixDagWalker(TokenGraph graph)
   {
      ArgumentNullException.ThrowIfNull(graph);

      _graph = graph;

      // Initialize walker at the root node
      _node = _graph.RootNode;
   }

   public void Convert(
       ReadOnlySpan<int> input,
       ref int inputIndex,
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      while (inputIndex < input.Length)
      {
         int tokenId = input[inputIndex];

         // Try to extend the current prefix with this tokenId
         if (_graph.TryGetTransition(_node, tokenId, out TokenGraphNode? next))
         {
            _node = next;
            inputIndex++;
            continue;
         }

         // No further prefix match:
         // Emit the merged token if available, otherwise emit the raw tokenId
         int emitId = _node.MergedTokenId >= 0
             ? _node.MergedTokenId
             : tokenId;

         if (outputIndex >= output.Length)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = emitId;

         // Reset to root and attempt to start a new prefix with the same tokenId
         _node = _graph.RootNode;

         if (_graph.TryGetTransition(_node, tokenId, out next))
         {
            _node = next;
         }

         inputIndex++;
      }
   }

   public void Flush(
       Span<int> output,
       ref int outputIndex,
       out bool completed)
   {
      completed = true;

      // If the current node represents a merged token, emit it
      if (_node.MergedTokenId >= 0) 
      {
         if (outputIndex >= output.Length)
         {
            completed = false;
            return;
         }

         output[outputIndex++] = _node.MergedTokenId;
      }

      // Reset walker state
      _node = _graph.RootNode;
   }
}
