namespace Brx.Hpc.Blocks;

public interface IBlockPool<T>
{
   bool HasFreeSlots { get; }

   BlockHandle<T> Allocate(int length);
   ReadOnlySpan<T> GetSpan(BlockHandle<T> handle);
   Span<T> GetWritableSpan(BlockHandle<T> handle);
   int GetLength(BlockHandle<T> handle);
   void SetLength(BlockHandle<T> handle, int length);
   void Release(BlockHandle<T> handle);
}

