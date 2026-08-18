namespace Brx.Hpc.Blocks;

public interface IBlockAllocator
{
   IBlockPool<T> CreatePool<T>(int capacity);
}
