namespace Brx.AI.Tokenizer.Wire;

public class Youtf75WireDecoder : AsciiSubsetWireDecoder
{
   public const string Alphabet =
      "\n\r\t " +
      "abcdefghijklmnopqrstuvwxyz" +
      "0123456789" +
      "!\"#$%&'()*+,-./" +
      ":;<=>?@" +
      "[\\]^_`" +
      "{|}~";

   public Youtf75WireDecoder(WireFallback fallback) 
      : base(Alphabet, fallback)
   {
   }
}
