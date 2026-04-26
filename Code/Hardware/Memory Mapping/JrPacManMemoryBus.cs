using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class JrPacManMemoryBus : IMemoryBus
{
    private byte[] _maincpu;
    private byte[] _spriteRam;
    private byte[] _spriteRam2;
    private NamcoWSG _wsg;

    public bool PlayingMsPacMan { get; set; }
    public bool SecondPlayerFlip { get; set; }
    public bool SteamDeckTwoPlayerMode { get; set; }
    public List<int> SubOptionIndices { get; set; } = [];
    
    public int HorizontalScroll { get; private set; }
    
    public JrPacManMemoryBus(byte[] spriteRam, byte[] spriteRam2, NamcoWSG wsg, byte[] maincpu)
    {
        _spriteRam = spriteRam;
        _spriteRam2 = spriteRam2;
        _wsg = wsg;
        _maincpu = maincpu;
    }
    
    public byte ReadByte(ushort address)
    {
        switch (address)
        {
            case >= 0x5000 and <= 0x503F: return GetPort0();
            case >= 0x5040 and <= 0x507F: return GetPort1();
            case >= 0x5080 and <= 0x50BF: return GetDipSwitchesFromOptions();
            
            default:
                return _maincpu[address];
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x4000 or >= 0x8000:
                return;
                
            case >= 0x5040 and <= 0x505F:
                _wsg.UpdateRegister(address, value);
                return;
            
            case 0x5003:
                SecondPlayerFlip = value == 1;
                return;
            
            case 0x5080:
                HorizontalScroll = value;
                return;
            
            case >= 0x5060 and <= 0x506F:
                _spriteRam2[address - 0x5060] = value;
                return;
        }

        if (address is >= 0x4000 and <= 0x4FFF)
        {
            _maincpu[address] = value;

            if (address is >= 0x4FF0 and <= 0x4FFF)
            {
                _spriteRam2[address - 0x4FF0] = value;
            }
        }
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

        dipSwitchValue |= (byte)SubOptionIndices[1];
        dipSwitchValue |= (byte)(SubOptionIndices[2] << 2);
        dipSwitchValue |= (byte)(SubOptionIndices[3] << 4);
        dipSwitchValue |= (byte)(SubOptionIndices[4] << 6);
        dipSwitchValue |= (byte)(SubOptionIndices[5] << 7);

        return dipSwitchValue;
    }
}