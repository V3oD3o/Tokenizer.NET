namespace Brx.Hpc.Blocks;

public interface IPassThroughBlockConverter<T> : IBlockConverter<T, T>
{
   bool CanPassThrough(ReadOnlySpan<T> input, int inputIndex);
}
