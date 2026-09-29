using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //one screen of the game (title, setup, match, results...)
    abstract class Scene : IDisposable
    {
        protected readonly Game1 game;

        protected Scene(Game1 game)
        {
            this.game = game;
        }

        //once per rendered frame: menu navigation
        public virtual void Update() { }
        //once per fixed simulation tick
        public virtual void Tick() { }
        //the music that plays on this screen
        public virtual MusicTrack Music => MusicTrack.Title;
        public abstract void Draw(SpriteBatch sb);
        public virtual void Dispose() { }

        //screens are laid out for a 700 pixel tall window and scaled to the real screen,
        //so full screen (for example 1920 x 1080) looks exactly like the window, only bigger
        public static float UiScale(GraphicsDevice gd) => G.UiScale(gd.Viewport.Width, gd.Viewport.Height);
        protected float Scale => UiScale(game.GraphicsDevice);
        protected Matrix Ui => Matrix.CreateScale(Scale);
        //the layout size (not the real pixel size) of the screen
        protected int Width => (int)Math.Round(game.GraphicsDevice.Viewport.Width / Scale);
        protected int Height => (int)Math.Round(game.GraphicsDevice.Viewport.Height / Scale);
        protected Vector2 Center => new Vector2(Width / 2f, Height / 2f);

        //the space background filling the screen, darkened so text is readable
        protected void DrawBackdrop(SpriteBatch sb, float darkness = 0.55f)
        {
            Texture2D bg = game.Background;
            float scale = Math.Max(Width / (float)bg.Width, Height / (float)bg.Height);
            sb.Draw(bg, Center, null, Color.White, 0, new Vector2(bg.Width / 2f, bg.Height / 2f), scale, SpriteEffects.None, 0);
            game.Prims.Rect(sb, new Rectangle(0, 0, Width, Height), Color.Black * darkness);
        }

        protected void DrawDisclaimer(SpriteBatch sb)
        {
            game.Font.DrawCentered(sb, "UNOFFICIAL FAN GAME - NOT AFFILIATED WITH OR ENDORSED BY NINTENDO",
                new Vector2(Width / 2f, Height - 18), Color.Gray, 2);
        }
    }

    //a vertical list of options
    class MenuList
    {
        readonly List<string> items;
        public int Selected { get; private set; }

        public MenuList(params string[] items)
        {
            this.items = new List<string>(items);
        }

        //returns the chosen index when confirmed, otherwise -1
        public int Update(MenuInput input, SoundBank audio)
        {
            if (input.Up())
            {
                Selected = (Selected + items.Count - 1) % items.Count;
                audio.Play(Sfx.MenuMove);
            }
            if (input.Down())
            {
                Selected = (Selected + 1) % items.Count;
                audio.Play(Sfx.MenuMove);
            }
            if (input.Confirm())
            {
                audio.Play(Sfx.MenuSelect);
                return Selected;
            }
            return -1;
        }

        public void Draw(SpriteBatch sb, PixelFont font, Vector2 center, float scale)
        {
            float lineHeight = 7 * scale + 14;
            float y = center.Y - (items.Count - 1) * lineHeight / 2;
            for (int i = 0; i < items.Count; i++)
            {
                bool sel = i == Selected;
                string text = sel ? "> " + items[i] + " <" : items[i];
                font.DrawCentered(sb, text, new Vector2(center.X, y + i * lineHeight), sel ? Color.Yellow : Color.White, scale);
            }
        }
    }
}
