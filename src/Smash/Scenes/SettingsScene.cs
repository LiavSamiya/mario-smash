using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //music and sound effect volume
    class SettingsScene : Scene
    {
        const int Rows = 3;
        int selected;

        public SettingsScene(Game1 game) : base(game) { }

        UserSettings Settings => game.Settings;

        public override void Update()
        {
            MenuInput input = game.Menu;
            if (input.Back())
            {
                Leave();
                return;
            }
            if (input.Up()) { selected = (selected + Rows - 1) % Rows; game.Audio.Play(Sfx.MenuMove); }
            if (input.Down()) { selected = (selected + 1) % Rows; game.Audio.Play(Sfx.MenuMove); }

            int delta = input.Right() ? 1 : input.Left() ? -1 : 0;
            if (selected == 2)
            {
                if (input.Confirm())
                    Leave();
                return;
            }
            if (delta == 0)
                return;
            if (selected == 0)
                Settings.MusicVolume = Math.Clamp(Settings.MusicVolume + delta, 0, UserSettings.MaxVolume);
            else
                Settings.SfxVolume = Math.Clamp(Settings.SfxVolume + delta, 0, UserSettings.MaxVolume);
            Settings.ApplyTo(game.Audio);
            //a sample so the new sound effect volume can be heard
            game.Audio.Play(selected == 1 ? Sfx.Jump : Sfx.MenuMove);
        }

        void Leave()
        {
            Settings.Save();
            game.Audio.Play(Sfx.MenuSelect);
            game.ChangeScene(new TitleScene(game));
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackdrop(sb, 0.7f);
            PixelFont font = game.Font;
            font.DrawCentered(sb, "SETTINGS", new Vector2(Width / 2f, 90), Color.Yellow, 8);

            DrawSlider(sb, 0, "MUSIC VOLUME", Settings.MusicVolume, Height * 0.38f);
            DrawSlider(sb, 1, "SOUND EFFECTS", Settings.SfxVolume, Height * 0.55f);
            bool back = selected == 2;
            font.DrawCentered(sb, back ? "> BACK <" : "BACK", new Vector2(Width / 2f, Height * 0.74f), back ? Color.Yellow : Color.White, 4);

            font.DrawCentered(sb, "UP/DOWN: SELECT    LEFT/RIGHT: CHANGE    ESC: BACK", new Vector2(Width / 2f, Height - 42), Color.LightGray, 2);
            sb.End();
        }

        void DrawSlider(SpriteBatch sb, int row, string label, int value, float y)
        {
            bool sel = selected == row;
            Color c = sel ? Color.Yellow : Color.White;
            game.Font.DrawCentered(sb, label, new Vector2(Width / 2f, y - 34), c, 3);

            const int segments = UserSettings.MaxVolume, segW = 34, gap = 6;
            int total = segments * segW + (segments - 1) * gap;
            int x0 = (Width - total) / 2;
            for (int i = 0; i < segments; i++)
            {
                var r = new Rectangle(x0 + i * (segW + gap), (int)y, segW, 22);
                game.Prims.Rect(sb, r, i < value ? (sel ? Color.Yellow : new Color(230, 70, 50)) : Color.White * 0.15f);
            }
            game.Font.Draw(sb, value.ToString(), new Vector2(x0 + total + 20, y), c, 3);
            if (sel)
            {
                game.Font.Draw(sb, "<", new Vector2(x0 - 40, y), c, 3);
                game.Font.Draw(sb, ">", new Vector2(x0 + total + 70, y), c, 3);
            }
        }
    }
}
