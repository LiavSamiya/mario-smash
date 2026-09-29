using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Smash
{
    //the MonoGame entry point: loads shared resources, runs the fixed-rate simulation and shows the current scene
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Scene scene;
        //real time not yet simulated
        double accumulator;

        internal Primitives Prims { get; private set; }
        internal PixelFont Font { get; private set; }
        internal SoundBank Audio { get; private set; }
        internal MenuInput Menu { get; } = new MenuInput();
        internal List<Character> Characters { get; private set; }
        internal Stage Stage { get; private set; }
        internal Texture2D Background { get; private set; }
        internal MatchSettings MatchSettings { get; } = new MatchSettings();
        internal UserSettings Settings { get; } = UserSettings.Load();
        internal bool ShowHitboxes { get => showHitboxes || ShowHitboxesOverride; private set => showHitboxes = value; }
        internal bool ShowHitboxesOverride { get; set; }
        bool showHitboxes;
        readonly string screenshotFolder;
        readonly Point captureSize;
        ScreenshotTour tour;

        public Game1(string screenshotFolder = null, Point captureSize = default)
        {
            this.screenshotFolder = screenshotFolder;
            this.captureSize = captureSize == default ? new Point(1920, 1080) : captureSize;
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = G.WindowWidth,
                PreferredBackBufferHeight = G.WindowHeight,
                SynchronizeWithVerticalRetrace = true,
            };
            //the game logic runs on its own fixed 60 Hz clock (see Update), so rendering can follow the monitor
            IsFixedTimeStep = false;
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.Title = "Mario Smash (unofficial fan game)";
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Prims = new Primitives(GraphicsDevice);
            Font = new PixelFont(GraphicsDevice);
            Audio = SoundBank.Create();
            Settings.ApplyTo(Audio);
            Characters = Character.LoadAll();
            if (Characters.Count == 0)
                throw new InvalidOperationException("No characters found in Content/Characters");
            Stage = Stage.LoadDefault();
            Stage.LoadTextures(GraphicsDevice);
            Background = ImageData.Load(System.IO.Path.Combine(G.ContentRoot, "Stages", Stage.Def.Background + ".png")).ToTexture(GraphicsDevice);
            if (screenshotFolder != null)
            {
                Audio.Muted = true;
                tour = new ScreenshotTour(this, screenshotFolder, captureSize);
            }
            ChangeScene(new TitleScene(this));
        }

        internal void ChangeScene(Scene next)
        {
            scene?.Dispose();
            scene = next;
            Audio.PlayMusic(next.Music);
            accumulator = 0;
        }

        protected override void Update(GameTime gameTime)
        {
            if (tour != null)
            {
                base.Update(gameTime);
                return;
            }
            Menu.Update();
            if (Menu.KeyPressed(Keys.F3))
                ShowHitboxes = !ShowHitboxes;
            if (Menu.KeyPressed(Keys.M))
                Audio.Muted = !Audio.Muted;
            if (Menu.KeyPressed(Keys.F11))
                ToggleFullScreen();

            Scene current = scene;
            current.Update();

            //run as many fixed ticks as the elapsed time needs, so the game speed does not depend on the frame rate
            accumulator += Math.Min(0.25, gameTime.ElapsedGameTime.TotalSeconds);
            while (accumulator >= G.TickSeconds && scene == current)
            {
                scene.Tick();
                accumulator -= G.TickSeconds;
            }
            base.Update(gameTime);
        }

        void ToggleFullScreen()
        {
            if (graphics.IsFullScreen)
            {
                graphics.IsFullScreen = false;
                graphics.PreferredBackBufferWidth = G.WindowWidth;
                graphics.PreferredBackBufferHeight = G.WindowHeight;
            }
            else
            {
                graphics.HardwareModeSwitch = false;
                graphics.IsFullScreen = true;
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            graphics.ApplyChanges();
        }

        protected override void Draw(GameTime gameTime)
        {
            if (tour != null)
            {
                if (tour.Done)
                    Exit();
                else
                    tour.CaptureNext(spriteBatch);
                return;
            }
            GraphicsDevice.Clear(Color.Black);
            scene.Draw(spriteBatch);
            base.Draw(gameTime);
        }
    }
}
