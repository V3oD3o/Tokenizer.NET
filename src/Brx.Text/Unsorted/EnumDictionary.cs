namespace Brx.Unsorted;

public sealed class EnumDictionary<TEnum, TValue>
    where TEnum : struct, Enum
{
   private readonly int _min;
   private readonly TValue[] _values;

   public EnumDictionary(Func<TEnum, TValue> valueFactory, int maxSpan = 1024)
   {
      ArgumentNullException.ThrowIfNull(valueFactory);

      var values = (TEnum[])Enum.GetValues(typeof(TEnum));

      int min = int.MaxValue;
      int max = int.MinValue;

      // Determine min and max underlying values
      foreach (var v in values)
      {
         int i = Convert.ToInt32(v);
         if (i < min) min = i;
         if (i > max) max = i;
      }

      int span = max - min + 1;

      if (span <= 0 || span > maxSpan)
      {
         throw new InvalidOperationException($"EnumDictionary span {span} is invalid or exceeds maxSpan {maxSpan}.");
      }

      _min = min;
      _values = new TValue[span];

      // Populate array
      foreach (var v in values)
      {
         int i = Convert.ToInt32(v);
         _values[i - _min] = valueFactory(v);
      }
   }

   public TValue this[TEnum key]
   {
      get
      {
         int i = Convert.ToInt32(key);
         return _values[i - _min];
      }
   }
}


