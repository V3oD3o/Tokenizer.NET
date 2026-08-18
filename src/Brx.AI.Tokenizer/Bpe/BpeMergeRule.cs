   namespace Brx.AI.Tokenizer.Bpe;

   public readonly struct BpeMergeRule
   {
      public readonly int LineNum;

      public readonly int Left;
      public readonly int Right;
      public readonly int Merged;

      public BpeMergeRule(int lineNum, int left, int right, int merged)
      {
         LineNum = lineNum;
      
         Left = left;
         Right = right;
         Merged = merged;
      }
   }
