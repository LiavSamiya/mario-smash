using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //one stock match. everything inside the match subscribes to its events, and Dispose()
    //unsubscribes all of them, so nothing from an old match keeps running
    class Match : IDisposable
    {
        public const int CountdownTicks = 3 * G.TicksPerSecond;

        //every object that updates once per tick
        public event DLG_update event_update;
        //every object drawn in the world
        public event DLG_draw event_draw;

        public Stage Stage { get; }
        public List<Fighter> Fighters { get; } = new List<Fighter>();
        public Camera Camera { get; }
        public Effects Effects { get; } = new Effects();
        public SoundBank Audio { get; }
        public MatchSettings Settings { get; }

        //drawing services (null when running without a window, e.g. in tests)
        public GraphicsDevice Graphics { get; }
        public Primitives Prims { get; }
        public PixelFont Font { get; }

        public Point ViewportSize { get; set; } = new Point(G.WindowWidth, G.WindowHeight);
        //how much bigger the real screen is than the 700 pixel tall layout (1.54 in full HD)
        public float ScreenScale => G.UiScale(ViewportSize.X, ViewportSize.Y);
        //the screen size in layout pixels: the camera frames the same area in a window and in full screen
        public Vector2 LayoutViewport => new Vector2(ViewportSize.X, ViewportSize.Y) / ScreenScale;
        public int Countdown { get; private set; }
        public bool Started => Countdown <= 0;
        public bool Over { get; private set; }
        public int OverTicks { get; private set; }
        public Fighter Winner { get; private set; }
        //fighters in the order they lost their last stock
        public List<Fighter> EliminationOrder { get; } = new List<Fighter>();
        public int ElapsedTicks { get; private set; }

        readonly CombatSystem combat;
        readonly MainFocus focus;

        public Match(MatchSettings settings, IReadOnlyList<Character> characters, Stage stage, SoundBank audio,
                     GraphicsDevice graphics = null, Primitives prims = null, PixelFont font = null, int seed = 0, bool skipCountdown = false,
                     IReadOnlyList<BaseKeys> inputOverride = null)
        {
            Settings = settings;
            Stage = stage;
            Audio = audio ?? SoundBank.Silent;
            Graphics = graphics;
            Prims = prims;
            Font = font;
            Countdown = skipCountdown ? 0 : CountdownTicks;
            combat = new CombatSystem(this);

            event_update += stage.Update;

            var active = Enumerable.Range(0, G.MaxPlayers).Where(i => settings.Slots[i].Type != SlotType.Off).ToList();
            for (int n = 0; n < active.Count; n++)
            {
                int i = active[n];
                SlotSettings slot = settings.Slots[i];
                BaseKeys keys;
                AiBrain brain = null;
                if (inputOverride != null)
                {
                    keys = inputOverride[n];
                }
                else if (slot.Type == SlotType.Cpu)
                {
                    brain = new AiBrain(slot.CpuLevel, seed * 31 + i);
                    keys = new BotKeys(brain);
                }
                else
                {
                    keys = MatchSettings.CreateKeys(slot.Device);
                }
                string tag = (slot.Type == SlotType.Cpu ? "CPU" : "P") + (i + 1);
                Character character = characters[Math.Clamp(slot.Character, 0, characters.Count - 1)];
                var fighter = new Fighter(this, i, character, slot.Palette, keys, MatchSettings.TagColors[i], tag,
                                          stage.SpawnPoint(n, active.Count), settings.Stocks);
                brain?.Attach(fighter, this);
                keys.Reset();
                Fighters.Add(fighter);
            }

            focus = new MainFocus(this);
            Camera = new Camera(focus, this);
        }

        public void Dispose()
        {
            foreach (Fighter f in Fighters)
                f.Dispose();
            event_update = null;
            event_draw = null;
        }

        //one fixed simulation step (1/60 of a second)
        public void Tick()
        {
            ElapsedTicks++;
            if (!Started)
            {
                if (Countdown % G.TicksPerSecond == 0)
                    Audio.Play(Sfx.Countdown);
                Countdown--;
                if (Started)
                    Audio.Play(Sfx.Go);
            }
            if (Over)
                OverTicks++;

            event_update?.Invoke();

            if (Started && !Over)
            {
                combat.Resolve(Fighters);
                CheckBlastZones();
                CheckGameOver();
            }
            Effects.Update();
        }

        void CheckBlastZones()
        {
            foreach (Fighter f in Fighters)
            {
                if (f.State == FighterState.Dead || Stage.BlastZone.Contains(f.Position))
                    continue;
                Box b = Stage.Bounds;
                Vector2 edge = Vector2.Clamp(f.Position, new Vector2(b.Left, b.Top), new Vector2(b.Right, b.Bottom));
                Effects.KoBlast(edge, f.TagColor);
                Audio.Play(Sfx.KO);
                Camera.Shake(10);
                f.KnockOut();
                if (f.Eliminated)
                    EliminationOrder.Add(f);
            }
        }

        void CheckGameOver()
        {
            var remaining = Fighters.Where(f => !f.Eliminated).ToList();
            if (remaining.Count > 1)
                return;
            Over = true;
            Winner = remaining.FirstOrDefault();
            Audio.Play(Sfx.Game);
        }

        //draws the world through the camera
        public void DrawWorld(SpriteBatch sb, bool showHitboxes)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.Transform);
            Stage.DrawBackground(sb);
            Stage.Draw(sb);
            event_draw?.Invoke(sb);
            Effects.Draw(sb, Prims);
            if (showHitboxes)
            {
                Prims.RectOutline(sb, Stage.Deck.ToRectangle(), Color.LimeGreen, 2);
                foreach (Fighter f in Fighters)
                    f.DrawDebug(sb);
            }
            sb.End();
        }
    }
}
