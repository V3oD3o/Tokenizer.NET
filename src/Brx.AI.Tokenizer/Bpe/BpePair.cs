namespace Brx.AI.Tokenizer.Bpe;

public readonly struct BpePair : IEquatable<BpePair>
{
   public readonly int Left;
   public readonly int Right;
   private readonly int _hashCode;

   public BpePair(int left, int right)
   {
      Left = left;
      Right = right;
      _hashCode = (left * 397) ^ right;
   }

   public bool Equals(BpePair other)
       => Left == other.Left && Right == other.Right;

   public override bool Equals(object? obj)
       => obj is BpePair other && Equals(other);

   public override int GetHashCode()
       => _hashCode;
}

