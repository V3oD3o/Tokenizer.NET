using Brx.AI.Tokenizer.Bpe;
using Brx.Hpc.Blocks;

namespace Brx.AI.Tokenizer.Wire;

public static class WireFormatCodec
{
   public static IBlockConverter<byte, char> CreateDecoder(WireFormat format)
   {
      return format switch
      {
         WireFormat.Ascii7 => new Ascii7WireDecoder(WireFallback.Throw),
         WireFormat.Ascii8MojiBake => new MojiBakeWireDecoder(),
         WireFormat.HuggingFaceMojiBake => new HuggingFaceMojiBakeWireDecoder(),
         WireFormat.Youtf75 => new Youtf75WireDecoder(WireFallback.Throw),
         WireFormat.Unicode => new UnicodeWireDecoder(WireFallback.Throw),

         _ => throw new ArgumentOutOfRangeException(nameof(format))
      };
   }

   public static IBlockConverter<byte, char> CreateDecoder(
      WireFormat format,
      WireFallback fallback)
   {
      ArgumentNullException.ThrowIfNull(fallback);

      return format switch
      {
         WireFormat.Ascii7 => new Ascii7WireDecoder(fallback),
         WireFormat.Ascii8MojiBake => new MojiBakeWireDecoder(),
         WireFormat.HuggingFaceMojiBake => new HuggingFaceMojiBakeWireDecoder(),
         WireFormat.Youtf75 => new Youtf75WireDecoder(fallback),
         WireFormat.Unicode => new UnicodeWireDecoder(fallback),

         _ => throw new ArgumentOutOfRangeException(nameof(format))
      };
   }
}
