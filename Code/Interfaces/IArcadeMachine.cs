using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
#if DEBUG
using MonoGame.ImGuiNet;
#endif

public interface IArcadeMachine
{
    short[] GetAudioSamples();
    void InitializeGraphics(GraphicsDevice device);
    int Step();
    List<DrawRequest> GetDrawRequests(bool secondPlayFlip);
    void TriggerVBlankInterrupt();
    #if DEBUG
    void DrawDebugUI(ImGuiRenderer renderer);
#endif
    
    int mode { get; set; }
    List<int> subOptionIndices { get; set; }
    bool secondPlayerFlip { get; }
}