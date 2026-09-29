using System;
using System.IO;

namespace Smash
{
    //delegate for all per-tick updates in a match
    public delegate void DLG_update();
    //delegate for all world drawing in a match
    public delegate void DLG_draw(Microsoft.Xna.Framework.Graphics.SpriteBatch sb);

    //global constants shared by the whole game
    static class G
    {
        //the simulation always runs at this rate, independent of the monitor refresh rate
        public const int TicksPerSecond = 60;
        public const double TickSeconds = 1.0 / TicksPerSecond;

        //default window size
        public const int WindowWidth = 1200;
        public const int WindowHeight = 700;

        //maximum number of fighters in a match
        public const int MaxPlayers = 4;

        //everything (menus, HUD, camera) is laid out for a WindowWidth x WindowHeight screen and scaled by this
        //factor to the real screen, so windowed and full screen at any size look the same
        public static float UiScale(int width, int height) => Math.Min(height / (float)WindowHeight, width / (float)WindowWidth);

        //root folder of all sprites, masks and data files
        public static string ContentRoot => Path.Combine(AppContext.BaseDirectory, "Content");
    }
}
