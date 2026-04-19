using System.Runtime.CompilerServices;

public static class ByteExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBit(this byte b, int index)
    {
        return (b >> index) & 1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte WithBitSet(this byte b, int index)
    {
        return (byte)(b | (1 << index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte WithBitCleared(this byte b, int index)
    {
        return (byte)(b & ~(1 << index));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(this byte b, int index)
    {
        return (b & (1 << index)) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte WithBitToggled(this byte b, int index)
    {
        return (byte)(b ^ (1 << index));
    }
}