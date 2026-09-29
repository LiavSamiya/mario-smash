using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    class ResultsScene : Scene
    {
        readonly List<Fighter> ranking;
        readonly Fighter winner;
        readonly AnimationPlayer anim = new AnimationPlayer();

        public ResultsScene(Game1 game, Match match) : base(game)
        {
            winner = match.Winner;
            //winner first, then the fighters that survived longest
            ranking = new List<Fighter>();
            if (winner != null)
                ranking.Add(winner);
            ranking.AddRange(Enumerable.Reverse(match.EliminationOrder).Where(f => f != winner));
            ranking.AddRange(match.Fighters.Where(f => !ranking.Contains(f)));
            if (winner != null)
                anim.Play(winner.Character.Animations[AnimationId.Stand]);
        }

        public override void Update()
        {
            if (game.Menu.Confirm())
            {
                game.Audio.Play(Sfx.MenuSelect);
                game.ChangeScene(new SetupScene(game));
            }
            else if (game.Menu.Back())
            {
                game.ChangeScene(new TitleScene(game));
            }
        }

        public override MusicTrack Music => MusicTrack.Battle;

        public override void Tick() => anim.Update();

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackdrop(sb, 0.6f);
            PixelFont font = game.Font;

            string title = winner == null ? "NO CONTEST" : winner.Tag + " WINS!";
            font.DrawCentered(sb, title, new Vector2(Width / 2f, 70), winner?.TagColor ?? Color.White, 9);

            if (winner != null)
            {
                SheetData data = anim.Current.Data;
                int frame = anim.SheetFrame;
                sb.Draw(data.Texture(game.GraphicsDevice, winner.PaletteIndex), new Vector2(Width * 0.18f, Height * 0.72f), data.Sheet.Frames[frame],
                    Color.White, 0, data.Sheet.Origins[frame], winner.Character.ScaleForHeight(280, 7), SpriteEffects.None, 0);
            }

            float x = Width * 0.36f;
            float y = 170;
            string[] headers = { "", "KOS", "FALLS", "DEALT", "TAKEN" };
            float[] columns = { 0, 190, 300, 420, 560 };
            for (int c = 0; c < headers.Length; c++)
                font.Draw(sb, headers[c], new Vector2(x + columns[c], y), Color.Gray, 3);
            y += 44;
            for (int i = 0; i < ranking.Count; i++)
            {
                Fighter f = ranking[i];
                FighterStats s = f.Stats;
                string[] values =
                {
                    (i + 1) + ". " + f.Tag,
                    s.KOs.ToString(),
                    s.Falls.ToString(),
                    (int)s.DamageDealt + "%",
                    (int)s.DamageTaken + "%",
                };
                for (int c = 0; c < values.Length; c++)
                    font.Draw(sb, values[c], new Vector2(x + columns[c], y), c == 0 ? f.TagColor : Color.White, 3.5f);
                y += 50;
            }

            font.DrawCentered(sb, "ENTER: PLAY AGAIN    ESC: TITLE", new Vector2(Width / 2f, Height - 60), Color.Yellow, 3);
            DrawDisclaimer(sb);
            sb.End();
        }
    }
}
