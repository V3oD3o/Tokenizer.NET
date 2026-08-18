namespace Brx.Hpc.Blocks;

public interface IBlockIteration<T>
{
   IEnumerable<BlockHandle<T>> GetBlocks(int maxConcurrentBlocks);

   bool CanReiterate { get; }
   bool HasTotalLength { get; }
   long TotalLength { get; }
}
