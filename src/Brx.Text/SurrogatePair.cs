namespace Brx.Text;

public readonly struct SurrogatePair : IEquatable<SurrogatePair>
{
   public readonly char High;
   public readonly char Low;

   public SurrogatePair(char high, char low)
   {
      High = high;
      Low = low;
   }

   public bool Equals(SurrogatePair other)
       => High == other.High && Low == other.Low;

   public override bool Equals(object obj)
       => obj is SurrogatePair sp && Equals(sp);

   public override int GetHashCode()
       => (High << 16) ^ Low;
}
