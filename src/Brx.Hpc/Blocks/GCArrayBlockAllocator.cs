using Brx.Hpc.Blocks;

public sealed class GCArrayBlockAllocator : IBlockAllocator
{
   internal GCArrayBlock<T> Allocate<T>(int length)
   {
      return new GCArrayBlock<T>
      {
         Array = new T[length],
         Length = length
      };
   }

   internal void Release<T>(ref GCArrayBlock<T> block)
   {
      block.Array = null;
      block.Length = 0;         
   }

   internal ReadOnlySpan<T> GetSpan<T>(in GCArrayBlock<T> block)
       => block.Array!.AsSpan(0, block.Length);   

   internal Span<T> GetWritableSpan<T>(in GCArrayBlock<T> block)
       => block.Array!.AsSpan(0, block.Length);   

   internal void SetLength<T>(ref GCArrayBlock<T> block, int length)   
   {
      if (block.Array is null)
         throw new InvalidOperationException("Block not allocated.");

      if (length < 0 || length > block.Array.Length)
         throw new ArgumentOutOfRangeException(nameof(length));

      block.Length = length;
   }

   public IBlockPool<T> CreatePool<T>(int capacity)
       => new GCArrayBlockPool<T>(this, capacity);
}
