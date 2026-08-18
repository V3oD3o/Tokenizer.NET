namespace Brx.Hpc.Blocks;

internal struct GCArrayBlock<T>
{
   public T[]? Array;
   public int Length;          // logical length

   public readonly bool IsAllocated => Array != null;
}

