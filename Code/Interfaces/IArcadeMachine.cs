using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using static PacManPCB;

public interface IArcadeMachine
{
    short[] GetAudioSamples();
    void InitializeGraphics(GraphicsDevice device);
    int Step(bool steppingThrough);
    List<DrawRequest> GetDrawRequests(bool secondPlayFlip);
    void TriggerVBlankInterrupt();
    
    int mode { get; set; }
    List<int> subOptionIndices { get; set; }
    bool secondPlayerFlip { get; }
    int graphicsViewerMode { get; set; }
    int tileViewerPaletteIndex { get; set; }
}