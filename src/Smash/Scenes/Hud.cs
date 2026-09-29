using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //damage percent, stocks, countdown and off-screen indicators, drawn in screen space
    class Hud
    {
        const int PanelWidth = 230;
        const int PanelHeight = 92;
        const int PanelGap = 18;

        readonly Game1 game;

        public Hud(Game1 game)
        {
            this.game = game;
        }

        public void Draw(SpriteBatch sb, Match match)
        {
            //laid out for a 700 pixel tall screen and scaled up, like the menus
            float scale = Scene.UiScale(game.GraphicsDevice);
            Viewport real = game.GraphicsDevice.Viewport;
            var vp = new Viewport(0, 0, (int)(real.Width / scale), (int)(real.Height / scale));
            PixelFont font = game.Font;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(scale));

            DrawOffscreenIndicators(sb, match, vp, scale);

            int count = match.Fighters.Count;
            int total = count * PanelWidth + (count - 1) * PanelGap;
            int x = (vp.Width - total) / 2;
            int y = vp.Height - PanelHeight - 14;
            foreach (Fighter f in match.Fighters)
            {
                DrawPanel(sb, f, new Rectangle(x, y, PanelWidth, PanelHeight));
                x += PanelWidth + PanelGap;
            }

            Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f - 60);
            if (!match.Started)
            {
                int n = (match.Countdown + G.TicksPerSecond - 1) / G.TicksPerSecond;
                font.DrawCentered(sb, n.ToString(), center, Color.White, 22);
            }
            else if (match.ElapsedTicks - Match.CountdownTicks < 50 && !match.Over)
            {
                font.DrawCentered(sb, "GO!", center, Color.Yellow, 20);
            }
            if (match.Over)
                font.DrawCentered(sb, "GAME!", center, Color.White, 20);

            sb.End();
        }

        void DrawPanel(SpriteBatch sb, Fighter f, Rectangle r)
        {
            Primitives prims = game.Prims;
            PixelFont font = game.Font;
            bool out_ = f.Eliminated;
            float dim = out_ ? 0.4f : 1;

            prims.Rect(sb, r, Color.Black * 0.6f);
            prims.Rect(sb, new Rectangle(r.X, r.Y, r.Width, 4), f.TagColor * dim);

            //portrait: the first frame of the standing animation
            Animation stand = f.Character.Animations[AnimationId.Stand];
            SpriteSheet sheet = stand.Data.Sheet;
            Rectangle src = sheet.Frames[stand.First];
            Texture2D tex = stand.Data.Texture(game.GraphicsDevice, f.PaletteIndex);
            sb.Draw(tex, new Vector2(r.X + 30, r.Bottom - 10), src, Color.White * dim, 0, sheet.Origins[stand.First], Math.Min(f.Character.ScaleForHeight(70), 50f / src.Width), SpriteEffects.None, 0);

            font.Draw(sb, f.Tag + " " + f.Character.Name.ToUpperInvariant(), new Vector2(r.X + 62, r.Y + 10), f.TagColor * dim, 2);

            if (out_)
            {
                font.Draw(sb, "OUT", new Vector2(r.X + 62, r.Y + 32), Color.Gray, 5);
                return;
            }

            string damage = (int)f.Damage + "%";
            font.Draw(sb, damage, new Vector2(r.X + 62, r.Y + 28), DamageColor(f.Damage), 5);

            //stock icons
            float iconScale = Math.Min(f.Character.ScaleForHeight(22), 16f / src.Width);
            int iconStep = (int)(src.Width * iconScale) + 2;
            int iconX = r.X + 64;
            int iconY = r.Bottom - 6;
            if (f.Stocks <= 5)
            {
                for (int i = 0; i < f.Stocks; i++)
                    sb.Draw(tex, new Vector2(iconX + i * iconStep, iconY), src, Color.White, 0, sheet.Origins[stand.First], iconScale, SpriteEffects.None, 0);
            }
            else
            {
                sb.Draw(tex, new Vector2(iconX, iconY), src, Color.White, 0, sheet.Origins[stand.First], iconScale, SpriteEffects.None, 0);
                font.Draw(sb, "X" + f.Stocks, new Vector2(iconX + iconStep, iconY - 14), Color.White, 2);
            }
        }

        //white at 0%, then yellow, red and dark red as damage builds up
        public static Color DamageColor(float damage)
        {
            if (damage < 60) return Color.Lerp(Color.White, new Color(255, 210, 70), damage / 60f);
            if (damage < 120) return Color.Lerp(new Color(255, 210, 70), new Color(255, 70, 40), (damage - 60) / 60f);
            return Color.Lerp(new Color(255, 70, 40), new Color(150, 0, 0), Math.Min(1, (damage - 120) / 80f));
        }

        //a bubble at the edge of the screen for fighters the camera cannot see
        void DrawOffscreenIndicators(SpriteBatch sb, Match match, Viewport vp, float scale)
        {
            const int inset = 30;
            foreach (Fighter f in match.Fighters)
            {
                if (f.State == FighterState.Dead)
                    continue;
                Vector2 p = match.Camera.WorldToScreen(f.Position - new Vector2(0, f.Character.BodyHeight / 2)) / scale;
                if (p.X >= 0 && p.X <= vp.Width && p.Y >= 0 && p.Y <= vp.Height)
                    continue;
                Vector2 c = new Vector2(MathHelper.Clamp(p.X, inset, vp.Width - inset), MathHelper.Clamp(p.Y, inset, vp.Height - inset));
                game.Prims.Circle(sb, c, 24, Color.Black * 0.6f);
                game.Prims.Circle(sb, c, 24, f.TagColor, filled: false);
                game.Font.DrawCentered(sb, f.Tag, c, f.TagColor, 2);
            }
        }
    }
}
