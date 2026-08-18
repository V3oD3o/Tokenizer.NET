namespace Brx.Text;

public sealed class SurrogateMappingInfo
{
   public bool HasMapping;
   public SurrogatePair Lower;
   public SurrogatePair Upper;

   public SurrogatePair Original;

   public bool IsMappableUpper => HasMapping && (Upper.High == Original.High) && (Upper.Low == Original.Low);

   public SurrogateMappingInfo(SurrogatePair original)
   {
      Original = original;
   }
}
