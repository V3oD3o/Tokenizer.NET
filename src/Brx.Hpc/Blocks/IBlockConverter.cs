namespace Brx.Hpc.Blocks;

public interface IBlockConverter<TIn, TOut>
{
   /// <summary>
   /// CONTRACT FOR IBlockConverter.Convert
   ///
   /// Convert MUST always make forward progress.
   /// Forward progress means:
   ///   - inputIndex increases, OR
   ///   - outputIndex increases.
   ///
   /// If completed == false:
   ///   - forward progress MUST have occurred,
   ///   - the caller (iterator) WILL retry Convert with a NEW output buffer,
   ///   - Convert MUST NOT throw,
   ///   - Convert MUST NOT consume further input after discovering that the
   ///     output buffer is full.
   ///
   /// Convert MAY return completed == false for two reasons:
   ///   1) The output buffer is full.
   ///   2) A multi-unit construct (e.g., surrogate pair, escape sequence)
   ///      is incomplete at the end of the input block.
   ///      In this case Convert MUST consume at least one input element
   ///      to satisfy the forward progress rule.
   ///
   /// Convert MAY maintain internal state (e.g., pending surrogate).
   /// Convert MUST resume processing this state on the next call.
   ///
   /// Convert MUST NOT assume that the entire stream is available.
   /// Convert MUST treat each input block independently and rely on
   /// the iterator to provide more input when completed == false.
   ///
   /// Convert MUST NOT emit invalid or partial output.
   /// Convert MUST NOT lose input data.
   ///
   ///
   /// CONTRACT FOR IBlockConverter.Flush
   ///
   /// Flush MUST complete all pending internal state.
   /// Flush MUST NOT consume input (there is none).
   ///
   /// Flush MAY return completed == false if the output buffer is full.
   /// In this case Flush MUST make forward progress (outputIndex increases).
   ///
   /// Flush MUST NOT leave unresolved state when completed == true.
   /// Flush MUST NOT throw due to incomplete multi-unit constructs.
   /// </summary>
   void Convert(
      ReadOnlySpan<TIn> input,
      ref int inputIndex,
      Span<TOut> output,
      ref int outputIndex,
      out bool completed
   );

   /// <summary>
   /// Flush any pending state at the end of the entire stream.
   /// Uses the same retry semantics as Convert.
   /// </summary>
   void Flush(
      Span<TOut> output,
      ref int outputIndex,
      out bool completed
   );
}
