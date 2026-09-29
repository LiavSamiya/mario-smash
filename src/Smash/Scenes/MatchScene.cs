using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    class MatchScene : Scene
    {
        //how long "GAME!" is shown before the results
        const int ResultsDelay = 150;

        readonly Match match;
        internal Match Match => match;
        readonly Hud hud;
        readonly MenuList pauseMenu = new MenuList("RESUME", "QUIT TO MENU");
        bool paused;

        public MatchScene(Game1 game) : base(game)
        {
            match = new Match(game.MatchSettings, game.Characters, game.Stage, game.Audio,
                              game.GraphicsDevice, game.Prims, game.Font, seed: Environment.TickCount);
            hud = new Hud(game);
        }

        public override void Update()
        {
            if (match.Over)
            {
                if (match.OverTicks >= ResultsDelay)
                    game.ChangeScene(new ResultsScene(game, match));
                return;
            }
            if (game.Menu.Pause())
            {
                paused = !paused;
                game.Audio.Play(Sfx.MenuSelect);
                return;
            }
            if (!paused)
                return;
            switch (pauseMenu.Update(game.Menu, game.Audio))
            {
                case 0: paused = false; break;
                case 1: game.ChangeScene(new SetupScene(game)); break;
            }
        }

        public override void Tick()
        {
            if (!paused)
                match.Tick();
        }

        public override void Draw(SpriteBatch sb)
        {
            match.ViewportSize = new Point(game.GraphicsDevice.Viewport.Width, game.GraphicsDevice.Viewport.Height);
            match.DrawWorld(sb, game.ShowHitboxes);
            hud.Draw(sb, match);
            if (!paused)
                return;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            game.Prims.Rect(sb, new Rectangle(0, 0, Width, Height), Color.Black * 0.6f);
            game.Font.DrawCentered(sb, "PAUSED", Center - new Vector2(0, 120), Color.White, 10);
            pauseMenu.Draw(sb, game.Font, Center + new Vector2(0, 20), 4);
            sb.End();
        }

        public override MusicTrack Music => MusicTrack.Battle;

        public override void Dispose() => match.Dispose();
    }
}
