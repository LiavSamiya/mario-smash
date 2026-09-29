using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //renders the README pictures and the frames of the gameplay video, at full screen resolution (1920 x 1080 by default)
    class ScreenshotTour
    {
        //frames of the gameplay video: one every VideoStep ticks (15 per second)
        const int VideoStep = 4;
        const int VideoFrames = 16 * G.TicksPerSecond / VideoStep;

        class Shot
        {
            public string File;
            public Func<Scene> Scene;
            public int Ticks;
            public bool Hitboxes;
            //more than 0: record this many frames into the "video" folder instead of one picture
            public int Frames;
        }

        readonly Game1 game;
        readonly string folder;
        readonly Point size;
        readonly Queue<Shot> shots = new Queue<Shot>();
        MatchScene match;

        public ScreenshotTour(Game1 game, string folder, Point size)
        {
            this.game = game;
            this.folder = folder;
            this.size = size;
            Directory.CreateDirectory(folder);
            ResetSlots();

            shots.Enqueue(new Shot { File = "title.png", Scene = () => new TitleScene(game), Ticks = 40 });
            shots.Enqueue(new Shot { File = "how-to-play.png", Scene = () => new HowToPlayScene(game), Ticks = 1 });
            shots.Enqueue(new Shot { File = "settings.png", Scene = () => new SettingsScene(game), Ticks = 1 });
            shots.Enqueue(new Shot { File = "setup.png", Scene = () => new SetupScene(game), Ticks = 20 });
            shots.Enqueue(new Shot { File = "character-select.png", Scene = TwoPlayerSelect, Ticks = 50 });
            shots.Enqueue(new Shot { File = "match-start.png", Scene = () => match = NewMatch(), Ticks = 100 });
            shots.Enqueue(new Shot { File = "match.png", Scene = () => match, Ticks = 20 * G.TicksPerSecond });
            shots.Enqueue(new Shot { File = "hitboxes.png", Scene = () => match, Ticks = 45, Hitboxes = true });
            shots.Enqueue(new Shot { File = "match-late.png", Scene = () => match, Ticks = 25 * G.TicksPerSecond });
            shots.Enqueue(new Shot { File = "results.png", Scene = FinishMatch, Ticks = 30 });
            //the gameplay video starts during the countdown of a fresh match
            shots.Enqueue(new Shot { File = "video", Scene = () => NewMatch(), Ticks = 100, Frames = VideoFrames });
        }

        //four CPU players, one of every character
        void ResetSlots()
        {
            MatchSettings s = game.MatchSettings;
            for (int i = 0; i < G.MaxPlayers; i++)
            {
                s.Slots[i].Type = SlotType.Cpu;
                s.Slots[i].CpuLevel = 3;
                s.Slots[i].Palette = 0;
                s.Slots[i].Character = i % game.Characters.Count;
            }
            s.Stocks = 2;
        }

        MatchScene NewMatch()
        {
            var scene = new MatchScene(game);
            //the camera needs the final screen size before the first tick
            scene.Match.ViewportSize = size;
            return scene;
        }

        //the usual two player character select, with player 2 still choosing
        Scene TwoPlayerSelect()
        {
            MatchSettings s = game.MatchSettings;
            for (int i = 0; i < G.MaxPlayers; i++)
                s.Slots[i].Type = i < 2 ? SlotType.Human : SlotType.Off;
            s.Slots[1].Character = 2;
            s.Slots[1].Palette = 3;
            var scene = new CharacterSelectScene(game);
            ResetSlots();
            return scene;
        }

        public bool Done => shots.Count == 0;

        //plays the match to the end and shows its results
        Scene FinishMatch()
        {
            for (int i = 0; i < 10 * 60 * G.TicksPerSecond && !match.Match.Over; i++)
                match.Tick();
            return new ResultsScene(game, match.Match);
        }

        //sets up the next scene, simulates it and saves one frame (or a whole video)
        public void CaptureNext(SpriteBatch sb)
        {
            Shot shot = shots.Dequeue();
            Scene scene = shot.Scene();
            game.ShowHitboxesOverride = shot.Hitboxes;
            for (int i = 0; i < shot.Ticks; i++)
                scene.Tick();

            if (shot.Frames == 0)
            {
                Save(scene, sb, Path.Combine(folder, shot.File));
                return;
            }
            string videoFolder = Path.Combine(folder, shot.File);
            Directory.CreateDirectory(videoFolder);
            for (int f = 0; f < shot.Frames; f++)
            {
                Save(scene, sb, Path.Combine(videoFolder, $"frame{f:0000}.png"), quiet: true);
                for (int i = 0; i < VideoStep; i++)
                    scene.Tick();
            }
            Console.WriteLine($"saved {shot.Frames} video frames in {videoFolder}");
        }

        void Save(Scene scene, SpriteBatch sb, string path, bool quiet = false)
        {
            GraphicsDevice gd = game.GraphicsDevice;
            using (var target = new RenderTarget2D(gd, size.X, size.Y))
            {
                gd.SetRenderTarget(target);
                gd.Clear(Color.Black);
                scene.Draw(sb);
                gd.SetRenderTarget(null);
                using (FileStream file = File.Create(path))
                    target.SaveAsPng(file, target.Width, target.Height);
            }
            if (!quiet)
                Console.WriteLine("saved " + path);
        }
    }
}
