public interface IMemoryBus
{
    byte ReadByte(ushort address);
    void WriteByte(ushort address, byte value);
    bool SecondPlayerFlip { get; set; }
    List<int> SubOptionIndices { get; set; }
    bool SteamDeckTwoPlayerMode { get; set; }
    bool PlayingMsPacMan { get; set; }
}