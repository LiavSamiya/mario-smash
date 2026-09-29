using System;

namespace Smash
{
    public static class Program
    {
        //  Smash                      play the game
        //  Smash --screenshots <dir> [--size 1920x1080]
        //      render the README pictures and gameplay video frames into <dir> at full screen resolution, then exit
        [STAThread]
        static void Main(string[] args)
        {
            string screenshots = null;
            int i = Array.IndexOf(args, "--screenshots");
            if (i >= 0)
                screenshots = i + 1 < args.Length ? args[i + 1] : "screenshots";

            var size = new Microsoft.Xna.Framework.Point(1920, 1080);
            int s = Array.IndexOf(args, "--size");
            if (s >= 0 && s + 1 < args.Length)
            {
                string[] wh = args[s + 1].Split('x');
                size = new Microsoft.Xna.Framework.Point(int.Parse(wh[0]), int.Parse(wh[1]));
            }

            using (var game = new Game1(screenshots, size))
                game.Run();
        }
    }
}
