namespace Brx.Text;

public sealed class CharMappingInfo
{
   public bool HasMapping;
   public char Lower;
   public char Upper;

   public bool IsMappableUpper => HasMapping && Upper == Original;

   public char Original;

   public CharMappingInfo(char original)
   {
      Original = original;
   }
}
