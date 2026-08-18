namespace Brx.Hpc.Blocks;

public readonly struct BlockHandle<T>
{
   public static readonly BlockHandle<T> Empty = new BlockHandle<T>();

   private readonly IBlockPool<T>? _pool;
   private readonly int _slot;

   public BlockHandle()
   {
      _pool = null;
      _slot = -1;
   }

   public BlockHandle(IBlockPool<T> pool, int slot)
   {
      ArgumentNullException.ThrowIfNull(pool);
      ArgumentOutOfRangeException.ThrowIfNegative(slot);

      _pool = pool;
      _slot = slot;
   }

   public int Slot => _slot;

   public int Length => _pool?.GetLength(this) ?? 0;

   public bool IsEmpty() => (_pool == null) || _pool.GetLength(this) == 0;

   public ReadOnlySpan<T> Span
   {
      get
      {
         if (_pool == null)
         {
            return ReadOnlySpan<T>.Empty;
         }
         return _pool.GetSpan(this);
      }
   }

   public void Release() => _pool?.Release(this);
}
