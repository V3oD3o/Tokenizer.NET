namespace Brx.AI.Tokenizer.Wire;

public enum WireFormat
{
   Ascii7,              // strict 7-bit ASCII only, 0x00..0x7F
   Ascii8MojiBake,      // direct (char)b, 0x00..0xFF allowed
   HuggingFaceMojiBake, // HuggingFace-style UTF-8 byte fallback mapping
   Youtf75,             // lowercase letters, digits, punctuation, whitespace only
   Unicode              // UTF-16 code units (Little Endian)
}
