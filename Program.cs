using Microsoft.Xna.Framework.Graphics;

using var game = new Game(args);
// Force SDL to use a smaller audio buffer on Linux
Environment.SetEnvironmentVariable("SDL_AUDIO_ALSA_SET_PERIOD_SIZE", "1");
game.Run();
