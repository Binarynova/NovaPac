using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;

public interface IArcadeMachine
{
    short[] GetAudioSamples();
    void InitializeGraphics(GraphicsDevice device);
    int Step(bool steppingThrough);
    List<DrawRequest> GetDrawRequests(bool secondPlayFlip);
    void TriggerVBlankInterrupt();
    void DrawDebugUI(ImGuiRenderer renderer);
    
    int mode { get; set; }
    List<int> subOptionIndices { get; set; }
    bool secondPlayerFlip { get; }
}