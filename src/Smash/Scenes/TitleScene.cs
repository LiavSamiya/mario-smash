using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    class TitleScene : Scene
    {
        readonly MenuList menu = new MenuList("START", "HOW TO PLAY", "SETTINGS", "QUIT");
        readonly AnimationPlayer[] anims;
        int ticks;

        public TitleScene(Game1 game) : base(game)
        {
            //the whole roster stands on the title screen
            anims = new AnimationPlayer[game.Characters.Count];
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i] = new AnimationPlayer();
                anims[i].Play(game.Characters[i].Animations[AnimationId.Stand]);
            }
        }

        public override void Update()
        {
            switch (menu.Update(game.Menu, game.Audio))
            {
                case 0: game.ChangeScene(new SetupScene(game)); break;
                case 1: game.ChangeScene(new HowToPlayScene(game)); break;
                case 2: game.ChangeScene(new SettingsScene(game)); break;
                case 3: game.Exit(); break;
            }
        }

        public override void Tick()
        {
            ticks++;
            foreach (AnimationPlayer a in anims)
                a.Update();
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackdrop(sb, 0.35f);

            //the fighters along the bottom, the left half facing right and the right half facing left
            int n = anims.Length;
            for (int i = 0; i < n; i++)
            {
                Character c = game.Characters[i];
                SheetData data = anims[i].Current.Data;
                int frame = anims[i].SheetFrame;
                float fx = n == 1 ? 0.5f : 0.1f + 0.8f * i / (n - 1);
                bool faceLeft = fx > 0.5f;
                Rectangle src = data.Sheet.Frames[frame];
                Vector2 origin = data.Sheet.Origins[frame];
                if (faceLeft)
                    origin.X = src.Width - origin.X;
                sb.Draw(data.Texture(game.GraphicsDevice, 0), new Vector2(Width * fx, Height * 0.9f), src, Color.White, 0, origin,
                    c.ScaleForHeight(160, 6), faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }

            float bob = (float)System.Math.Sin(ticks * 0.05) * 4;
            game.Font.DrawCentered(sb, "MARIO SMASH", new Vector2(Width / 2f + 5, Height * 0.17f + bob + 5), new Color(120, 10, 10), 11, shadow: false);
            game.Font.DrawCentered(sb, "MARIO SMASH", new Vector2(Width / 2f, Height * 0.17f + bob), new Color(255, 70, 60), 11, shadow: false);
            game.Font.DrawCentered(sb, "A FAN-MADE PLATFORM FIGHTER", new Vector2(Width / 2f, Height * 0.17f + 70), Color.White, 3);
            menu.Draw(sb, game.Font, new Vector2(Width / 2f, Height * 0.48f), 4);
            DrawDisclaimer(sb);
            sb.End();
        }
    }
}
