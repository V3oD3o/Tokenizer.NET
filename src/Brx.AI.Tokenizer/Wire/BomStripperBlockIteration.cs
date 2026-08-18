using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire
{
   public sealed class BomStripperBlockIteration : IBlockIteration<char>
   {
      private readonly IBlockIteration<char> _input;
      private readonly IBlockAllocator _allocator;

      public BomStripperBlockIteration(IBlockIteration<char> source, IBlockAllocator allocator)
      {
         ArgumentNullException.ThrowIfNull(source);
         ArgumentNullException.ThrowIfNull(allocator);

         _input = source;
         _allocator = allocator;
      }

      public bool CanReiterate => _input.CanReiterate;

      public bool HasTotalLength => false;

      public long TotalLength => throw new NotSupportedException();

      public IEnumerable<BlockHandle<char>> GetBlocks(int maxConcurrentBlocks)
      {
         bool first = true;

         foreach (BlockHandle<char> block in _input.GetBlocks(0))
         {
            if (block.IsEmpty())
            {
               continue;
            }

            if (first)
            {
               first = false;

               var span = block.Span;
               if (span[0] == '\uFEFF')
               {
                  if (span.Length > 1)
                  {
                     var tmpPool = _allocator.CreatePool<char>(1);
                     var bomFreeBlock = tmpPool.Allocate(span.Length - 1);
                     span.Slice(1).CopyTo(tmpPool.GetWritableSpan(bomFreeBlock));
                     yield return bomFreeBlock;
                  }
                  block.Release();
                  continue;
               }
            }

            yield return block;
            block.Release();
         }
      }
   }
}
