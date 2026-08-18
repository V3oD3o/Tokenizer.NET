using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Brx.AI.Tokenizer.Bpe;

public sealed class BpeVocab
{
   private readonly Dictionary<string, int> _tokenToId;
   private readonly string[] _idToToken;

   // Fast lookup table for U+0000..U+0140 (0..320)
   // Size = 321
   private readonly int[] _fastTokenIds = new int[321];

   public ReadOnlyDictionary<string, int> TokenToId { get; }
   public ReadOnlyCollection<string> IdToToken { get; }
   public int MaxTokenLength { get; }

   private BpeVocab(Dictionary<string, int> tokenToId, string[] idToToken, int maxTokenLength)
   {
      _tokenToId = tokenToId;
      _idToToken = idToToken;

      TokenToId = new ReadOnlyDictionary<string, int>(_tokenToId);
      IdToToken = new ReadOnlyCollection<string>(_idToToken);

      MaxTokenLength = maxTokenLength;
   }

   public static BpeVocab Load(string vocabJsonPath)
   {
      ArgumentNullException.ThrowIfNull(vocabJsonPath);

      using var reader = File.OpenText(vocabJsonPath);
      return Load(reader);
   }

   public static BpeVocab Load(TextReader reader)
   {
      ArgumentNullException.ThrowIfNull(reader);

      using var json = new JsonTextReader(reader);
      return Load(json);
   }

   public static BpeVocab Load(JsonTextReader json)
   {
      var tokenToId = new Dictionary<string, int>(StringComparer.Ordinal);
      int maxId = -1;
      int maxTokenLen = -1;

      if (!json.Read() || json.TokenType != JsonToken.StartObject)
         throw new InvalidOperationException("Invalid vocab.json: expected object");

      while (json.Read())
      {
         if (json.TokenType == JsonToken.PropertyName)
         {
            if (json.Value is not string token)
               throw new InvalidOperationException("Invalid vocab.json: expected string token");

            if (!json.Read() || json.TokenType != JsonToken.Integer)
               throw new InvalidOperationException("Invalid vocab.json: expected integer value");

            int id = Convert.ToInt32(json.Value);

            if (!tokenToId.TryAdd(token, id))
               throw new InvalidOperationException($"Duplicate token in vocab.json: '{token}'");

            if (id > maxId)
               maxId = id;

            if (token.Length > maxTokenLen)
               maxTokenLen = token.Length;
         }
         else if (json.TokenType == JsonToken.EndObject)
         {
            break;
         }
      }

      if (maxId < 0)
         throw new InvalidOperationException("Empty vocab.json");

      var idToToken = new string[maxId + 1];
      foreach (var kv in tokenToId)
      {
         if (kv.Key is null)
            throw new InvalidOperationException("Null token in vocab.json");

         int id = kv.Value;

         if (idToToken[id] != null)
            throw new InvalidOperationException($"Duplicate token-id in vocab.json: {id}");

         idToToken[id] = kv.Key;
      }

      return new BpeVocab(tokenToId, idToToken, maxTokenLen);
   }

   public bool TryGetId(string token, out int id)
   {
      return _tokenToId.TryGetValue(token, out id);
   }

   public string GetTokenString(int id)
   {
      return _idToToken[id];
   }

   // ---------------------------------------------------------
   // FAST LOOKUP: U+0000..U+0140 (0..320)
   // index = codepoint
   // 0 = unknown -> lazy lookup
   // ---------------------------------------------------------
   private int ResolveCharInternal(char ch)
   {
      int code = (int)ch;

      if (code <= 320)
      {
         int cached = _fastTokenIds[code];
         if (cached != 0)
            return cached;

         string token = ch.ToString();

         if (!_tokenToId.TryGetValue(token, out int id))
            throw new InvalidOperationException(
               $"Missing token for char U+{code:X4} ('{token}')");

         _fastTokenIds[code] = id;
         return id;
      }

      // Fallback: dictionary lookup for all other Unicode chars
      string t = ch.ToString();

      if (!_tokenToId.TryGetValue(t, out int unicodeId))
         throw new InvalidOperationException(
            $"Missing token for Unicode char U+{code:X4} ('{t}')");

      return unicodeId;
   }

   public int GetCharTokenId(char ch)
   {
      return ResolveCharInternal(ch);
   }

   public int GetByteTokenId(byte b)
   {
      char ch = (char)b;
      return ResolveCharInternal(ch);
   }
}
