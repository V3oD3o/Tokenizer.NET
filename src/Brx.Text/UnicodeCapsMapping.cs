namespace Brx.Text;

public static class UnicodeCapsMapping
{
   private static readonly CharMappingInfo[] FastBmpCache = new CharMappingInfo[512];
   private static readonly Dictionary<char, CharMappingInfo> BmpMap = new();
   private static readonly Dictionary<SurrogatePair, SurrogateMappingInfo> SurrogateMap = new();

   // ---------------------------------------------------------
   // BMP
   // ---------------------------------------------------------
   public static CharMappingInfo Get(char ch)
   {
      // Fast-path
      if (ch < FastBmpCache.Length)
      {
         var info = FastBmpCache[ch];
         if (info != null)
            return info;

         info = ComputeBmpMapping(ch);
         FastBmpCache[ch] = info;
         return info;
      }

      // Slow-path
      if (BmpMap.TryGetValue(ch, out var cached))
         return cached;

      var newInfo = ComputeBmpMapping(ch);
      BmpMap[ch] = newInfo;
      return newInfo;
   }

   private static CharMappingInfo ComputeBmpMapping(char ch)
   {
      var info = new CharMappingInfo(ch);

      string s = ch.ToString();
      string lower = s.ToLowerInvariant();
      string upper = s.ToUpperInvariant();

      if (lower == upper || lower.Length != 1 || upper.Length != 1)
      {
         info.HasMapping = false;
         return info;
      }

      info.HasMapping = true;
      info.Lower = lower[0];
      info.Upper = upper[0];
      return info;
   }

   // ---------------------------------------------------------
   // Surrogate
   // ---------------------------------------------------------
   public static SurrogateMappingInfo Get(char high, char low)
   {
      var pair = new SurrogatePair(high, low);

      if (SurrogateMap.TryGetValue(pair, out var cached))
         return cached;

      var info = ComputeSurrogateMapping(pair);
      SurrogateMap[pair] = info;
      return info;
   }

   private static SurrogateMappingInfo ComputeSurrogateMapping(SurrogatePair pair)
   {
      var info = new SurrogateMappingInfo(pair);

      string s = new([pair.High, pair.Low]);
      string lowerStr = s.ToLowerInvariant();
      string upperStr = s.ToUpperInvariant();

      if (lowerStr == upperStr || lowerStr.Length != 2 || upperStr.Length != 2)
      {
         info.HasMapping = false;
         return info;
      }

      info.HasMapping = true;
      info.Lower = new SurrogatePair(lowerStr[0], lowerStr[1]);
      info.Upper = new SurrogatePair(upperStr[0], upperStr[1]);
      return info;
   }
}
