using System.Diagnostics;

namespace Brx.Hpc.Blocks;

internal sealed class GCArrayBlockPool<T> : IBlockPool<T>
{
   private readonly GCArrayBlockAllocator _allocator;
   private readonly GCArrayBlock<T>[] _slots;
   private int _head;
   private int _freeCount;

   public bool HasFreeSlots => (_freeCount > 0);

   public GCArrayBlockPool(GCArrayBlockAllocator allocator, int capacity)                                                           
   {
      _allocator = allocator;
      _slots = new GCArrayBlock<T>[capacity];
      _head = 0;
      _freeCount = capacity;
   }

   public BlockHandle<T> Allocate(int length)
   {
      // Fast path: ring-buffer slot is free
      ref var hb = ref _slots[_head];
      if (!hb.IsAllocated)
      {
         hb = _allocator.Allocate<T>(length);
         Debug.Assert(hb.IsAllocated);
         _freeCount--;

         // advance head
         _head++;
         if (_head >= _slots.Length)
            _head = 0;

         return new BlockHandle<T>(this, _head);
      }

      if (_freeCount > 0)
      {
         // Slow path: fallback linear search starting from head
         for (int i = _head; i < _slots.Length; i++)
         {
            ref var blk = ref _slots[i];
            if (!blk.IsAllocated)
            {
               blk = _allocator.Allocate<T>(length);
               Debug.Assert(blk.IsAllocated);
               _freeCount--;
               return new BlockHandle<T>(this, i);
            }
         }

         // Wrap-around search
         for (int i = 0; i < _head; i++)
         {
            ref var blk = ref _slots[i];
            if (!blk.IsAllocated)
            {
               blk = _allocator.Allocate<T>(length);
               Debug.Assert(blk.IsAllocated);
               _freeCount--;
               return new BlockHandle<T>(this, i);
            }
         }
      }

      throw new InvalidOperationException("No free block slots.");
   }

   internal ref GCArrayBlock<T> GetDescriptor(int slotIndex)
       => ref _slots[slotIndex];

   public ReadOnlySpan<T> GetSpan(BlockHandle<T> handle)
       => _allocator.GetSpan(in _slots[handle.Slot]);

   public Span<T> GetWritableSpan(BlockHandle<T> handle)
       => _allocator.GetWritableSpan(in _slots[handle.Slot]);

   public int GetLength(BlockHandle<T> handle)
      => _slots[handle.Slot].Length;

   public void SetLength(BlockHandle<T> handle, int length)
   {
      _allocator.SetLength(ref _slots[handle.Slot], length);
   }

   public void Release(BlockHandle<T> handle)
   {
      ref var blk = ref _slots[handle.Slot];

      if (blk.IsAllocated)
      {
         _allocator.Release(ref blk);
         if (!blk.IsAllocated)
         {
            _freeCount++;
         }
      }
   }
}
