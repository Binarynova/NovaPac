using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class PacManMemoryMap : IMemoryBus
{
    private byte[] _memory;
    private byte[] _auxRoms;
    private byte[] _spriteRam;
    private byte[] _spriteRam2;
    private NamcoWSG _wsg;

    public bool AuxBoardEnabled { get; set; }
    public bool DecryptEnabled { get; set; }
    public bool SecondPlayerFlip { get; set; }
    public bool SteamDeckTwoPlayerMode { get; set; }
    public List<int> SubOptionIndices { get; set; } = [];
    
    public PacManMemoryMap(byte[] memory, byte[] auxRoms, byte[] spriteRam, byte[] spriteRam2, NamcoWSG wsg)
    {
        _memory = memory;
        _auxRoms = auxRoms;
        _spriteRam = spriteRam;
        _spriteRam2 = spriteRam2;
        _wsg = wsg;
    }
    
    public byte ReadByte(ushort address)
    {
        if (AuxBoardEnabled)
        {
            switch (address)
            {
                case >= 0x3FF8 and <= 0x3FFF:
                    DecryptEnabled = true;
                    break;
                case >= 0x0038 and <= 0x003F:
                case >= 0x03B0 and <= 0x03B7:
                case >= 0x1600 and <= 0x1607:
                case >= 0x2120 and <= 0x2127:
                case >= 0x3FF0 and <= 0x3FF7:
                case >= 0x8000 and <= 0x8007:
                case >= 0x97F0 and <= 0x97F7:
                    DecryptEnabled = false;
                    break;
            }

            switch (address)
            {
                case < 0x4000:
                    return DecryptEnabled ? _auxRoms[address] : _memory[address];
                case >= 0x8000 and < 0x8800:
                    return _auxRoms[address - 0x8000 + 0x6000];
                case >= 0x8800 and < 0xA000:
                    return _auxRoms[(address & 0xFFF) + 0x5000];
            }
        }
        
        switch (address)
        {
            case >= 0x5000 and <= 0x503F:
                return GetPort0();
            case >= 0x5040 and <= 0x507F:
                return GetPort1();
            case >= 0x5080 and <= 0x50BF:
                return GetDipSwitchesFromOptions(); // DIPs
            default:
                return _memory[NormalizeAddress(address)];
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        if (AuxBoardEnabled)
        {
            switch (address)
            {
                case >= 0x3FF8 and <= 0x3FFF:
                    DecryptEnabled = true;
                    break;
                case >= 0x0038 and <= 0x003F:
                case >= 0x03B0 and <= 0x03B7:
                case >= 0x1600 and <= 0x1607:
                case >= 0x2120 and <= 0x2127:
                case >= 0x3FF0 and <= 0x3FF7:
                case >= 0x8000 and <= 0x8007:
                case >= 0x97F0 and <= 0x97F7:
                    DecryptEnabled = false;
                    break;
            }

            if (address is >= 0x8000 and < 0xA000)
                return;
        }
        
        switch (address)
        {
            case < 0x4000:
                return; // Protect ROM
            case >= 0x5040 and <= 0x505F: // Intercept sound writes
                _wsg.UpdateRegister(address, value);
                return;
            case 0x5003: // intercept flip
                SecondPlayerFlip = value == 1;
                break;
            case >= 0x5060 and <= 0x506F: // Handle special non-mirrored I/O writes (Sync bus)
                _spriteRam2[address - 0x5060] = value;
                return;
        }

        ushort normAddr = NormalizeAddress(address);

        // Sync Sprite RAM 1
        // 0x4FF0 is the canonical location for sprite data
        if (normAddr is >= 0x4FF0 and <= 0x4FFF)
        {
            _spriteRam[normAddr - 0x4FF0] = value;
        }

        // Store the value in our normalized "canonical" RAM block
        _memory[normAddr] = value;
    }
    
    private static ushort NormalizeAddress(ushort address)
    {
        if (address < 0x4000 || (address >= 0x5000 && address <= 0x50FF))
        {
            return address;
        }

        // Everything else (0x4000-0x4FFF, 0x8000-0x8FFF, 0xC000-0xCFFF, etc.)
        // maps down to the primary 4KB RAM block at 0x4000.
        // (address & 0x0FFF) gets the offset within any 4KB bank.
        return (ushort)(0x4000 | (address & 0x0FFF));
    }

    private byte GetPort0()
    {
        KeyboardState state = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        if (SteamDeckTwoPlayerMode)
        {
            return (byte)
            (
                ((state.IsKeyDown(Keys.Up) || gamePadState.DPad.Up == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y >= 0.5f ? 0 : 1) << 1)
                | ((state.IsKeyDown(Keys.Left) || gamePadState.DPad.Left == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X <= -0.5f ? 0 : 1) << 3)
                | ((state.IsKeyDown(Keys.Right) || gamePadState.DPad.Right == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X >= 0.5f ? 0 : 1) << 0)
                | ((state.IsKeyDown(Keys.Down) || gamePadState.DPad.Down == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y <= -0.5f ? 0 : 1) << 2)
                | ((state.IsKeyDown(Keys.S) ? 0 : 1) << 4)
                | ((state.IsKeyDown(Keys.C) || gamePadState.Buttons.Back == ButtonState.Pressed ? 0 : 1) << 5)
                | ((state.IsKeyDown(Keys.D) ? 0 : 1) << 6)
                | ((state.IsKeyDown(Keys.M) ? 0 : 1) << 7)
            );
        }
        
        return (byte)
        (
            ((state.IsKeyDown(Keys.Up) || gamePadState.DPad.Up == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y >= 0.5f ? 0 : 1) << 0)
            | ((state.IsKeyDown(Keys.Left) || gamePadState.DPad.Left == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X <= -0.5f ? 0 : 1) << 1)
            | ((state.IsKeyDown(Keys.Right) || gamePadState.DPad.Right == ButtonState.Pressed || gamePadState.ThumbSticks.Left.X >= 0.5f ? 0 : 1) << 2)
            | ((state.IsKeyDown(Keys.Down) || gamePadState.DPad.Down == ButtonState.Pressed || gamePadState.ThumbSticks.Left.Y <= -0.5f ? 0 : 1) << 3)
            | ((state.IsKeyDown(Keys.S) ? 0 : 1) << 4)
            | ((state.IsKeyDown(Keys.C) || gamePadState.Buttons.Back == ButtonState.Pressed ? 0 : 1) << 5)
            | ((state.IsKeyDown(Keys.D) ? 0 : 1) << 6)
            | ((state.IsKeyDown(Keys.M) ? 0 : 1) << 7)
        );
    }
    
    private byte GetPort1()
    {
        KeyboardState state = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        if (SteamDeckTwoPlayerMode)
        {
            return (byte)
            (
                ((state.IsKeyDown(Keys.NumPad8) || gamePadState.ThumbSticks.Right.Y >= 0.5f ? 0 : 1) << 2)
                | ((state.IsKeyDown(Keys.NumPad4) || gamePadState.ThumbSticks.Right.X <= -0.5f ? 0 : 1) << 0)
                | ((state.IsKeyDown(Keys.NumPad6) || gamePadState.ThumbSticks.Right.X >= 0.5f ? 0 : 1) << 3)
                | ((state.IsKeyDown(Keys.NumPad2) || gamePadState.ThumbSticks.Right.Y <= -0.5f ? 0 : 1) << 1)
                | ((state.IsKeyDown(Keys.T) ? 0 : 1) << 4)
                | ((state.IsKeyDown(Keys.Enter) || gamePadState.Buttons.Start == ButtonState.Pressed ? 0 : 1) << 5)
                | ((state.IsKeyDown(Keys.Tab) ? 0 : 1) << 6)
                | (0x0 << 7) // 1 for upright, 0 for cocktail
            );
        }
        
        return (byte)
        (
            ((state.IsKeyDown(Keys.NumPad8) ? 0 : 1) << 0)
            | ((state.IsKeyDown(Keys.NumPad4) ? 0 : 1) << 1)
            | ((state.IsKeyDown(Keys.NumPad6) ? 0 : 1) << 2)
            | ((state.IsKeyDown(Keys.NumPad2) ? 0 : 1) << 3)
            | ((state.IsKeyDown(Keys.T) ? 0 : 1) << 4)
            | ((state.IsKeyDown(Keys.Enter) || gamePadState.Buttons.Start == ButtonState.Pressed ? 0 : 1) << 5)
            | ((state.IsKeyDown(Keys.Tab) ? 0 : 1) << 6)
            | (0x1 << 7) // 1 for upright, 0 for cocktail
        );
    }

    public byte GetDipSwitchesFromOptions()
    {
        byte dipSwitchValue = 0x00;

        dipSwitchValue |= (byte)SubOptionIndices[0];
        dipSwitchValue |= (byte)(SubOptionIndices[1] << 2);
        dipSwitchValue |= (byte)(SubOptionIndices[2] << 4);
        dipSwitchValue |= (byte)(SubOptionIndices[3] << 5);
        dipSwitchValue |= (byte)(SubOptionIndices[4] << 6);

        return dipSwitchValue;
    }
}