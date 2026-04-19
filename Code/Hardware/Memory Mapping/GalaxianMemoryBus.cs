using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class GalaxianMemoryBus : IMemoryBus
{
    private byte[] _memory;

    public bool NmiEnabled { get; private set; }
    public bool SecondPlayerFlip { get; set; }
    public bool SteamDeckTwoPlayerMode { get; set; }
    public List<int> SubOptionIndices { get; set; } = [];
    
    public GalaxianMemoryBus(byte[] memory)
    {
        _memory = memory;
    }
    
    public byte ReadByte(ushort address)
    {
        if (address == 0x0000)
            Console.WriteLine("__________________Back to Start");
        return _memory[address];
    }

    public void WriteByte(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x4000:
                return; // Protect ROM
            case 0x7001:
                NmiEnabled = (value & 0x01) != 0;
                Console.WriteLine($"NMI State Changed: {NmiEnabled}");
                break;
            case > 0x5000 and < 0x5400:
                if (value != 0x10)
                    Console.WriteLine("Something different written to vram.");
                break;
        }

        _memory[address] = value;
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

        dipSwitchValue |= (byte)SubOptionIndices[2];
        dipSwitchValue |= (byte)(SubOptionIndices[3] << 2);
        dipSwitchValue |= (byte)(SubOptionIndices[4] << 4);
        dipSwitchValue |= (byte)(SubOptionIndices[5] << 6);
        dipSwitchValue |= (byte)(SubOptionIndices[6] << 7);

        return dipSwitchValue;
    }
}