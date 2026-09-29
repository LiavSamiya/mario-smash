using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //Tekken-inspired character select: a row of portraits, a big animated preview of every player's fighter,
    //a countdown timer and its own music. every human moves their own cursor with their own controls:
    //left/right = fighter, up/down = skin colour, attack = select, shield = cancel.
    //when all humans are ready, player 1 picks for the CPU players
    class CharacterSelectScene : Scene
    {
        const int TimeLimit = 30 * G.TicksPerSecond;
        const int ReadyDelay = 75;

        class Picker
        {
            public int Slot;
            public bool Cpu;
            public BaseKeys Keys;
            public int Cursor;
            public int Palette;
            public bool Locked;
            public readonly AnimationPlayer Anim = new AnimationPlayer();
        }

        readonly List<Picker> pickers = new List<Picker>();
        int timer = TimeLimit;
        int readyTicks = -1;
        int ticks;

        public CharacterSelectScene(Game1 game) : base(game)
        {
            MatchSettings settings = game.MatchSettings;
            for (int i = 0; i < G.MaxPlayers; i++)
            {
                SlotSettings s = settings.Slots[i];
                if (s.Type == SlotType.Off)
                    continue;
                var p = new Picker
                {
                    Slot = i,
                    Cpu = s.Type == SlotType.Cpu,
                    Keys = s.Type == SlotType.Human ? MatchSettings.CreateKeys(s.Device) : null,
                    Cursor = Math.Clamp(s.Character, 0, game.Characters.Count - 1),
                    Palette = s.Palette,
                };
                p.Keys?.Reset();
                p.Anim.Play(Fighter(p).Animations[AnimationId.Stand]);
                pickers.Add(p);
            }
            //with no human to choose for them, CPU players keep the fighters from the last match
            if (pickers.All(p => p.Cpu))
                foreach (Picker p in pickers)
                    Lock(p);
        }

        public override MusicTrack Music => MusicTrack.Select;

        Character Fighter(Picker p) => game.Characters[p.Cursor];

        //the human picking right now for a CPU (the first human), or null
        Picker CpuController => pickers.FirstOrDefault(p => !p.Cpu);

        //the CPU player that is being picked for, once every human is ready
        Picker CurrentCpu => pickers.Where(p => !p.Cpu).All(p => p.Locked) ? pickers.FirstOrDefault(p => p.Cpu && !p.Locked) : null;

        public override void Update()
        {
            if (game.Menu.Back() && pickers.All(p => !p.Locked))
            {
                game.ChangeScene(new SetupScene(game));
                return;
            }
            if (readyTicks >= 0)
                return;

            foreach (Picker p in pickers.Where(p => !p.Cpu))
                p.Keys.Update();

            foreach (Picker p in pickers.Where(p => !p.Cpu))
            {
                Picker target = p;
                //player 1 controls the CPU cursors after locking in
                if (p.Locked && p == CpuController && CurrentCpu != null)
                    target = CurrentCpu;
                HandleInput(p.Keys, target, p);
            }

            //enter also selects, for whoever is choosing first
            if (game.Menu.Confirm())
            {
                Picker next = pickers.FirstOrDefault(p => !p.Cpu && !p.Locked) ?? CurrentCpu;
                if (next != null)
                    Lock(next);
            }
        }

        void HandleInput(BaseKeys keys, Picker target, Picker owner)
        {
            if (keys.Pressed(InputButtons.Shield))
            {
                //cancel the last choice: the CPU picked before, or this player's own
                Picker undo = owner == CpuController
                    ? pickers.LastOrDefault(p => p.Cpu && p.Locked) ?? (owner.Locked ? owner : null)
                    : (owner.Locked ? owner : null);
                if (target != owner && !target.Locked && undo == null)
                    undo = owner;
                if (undo != null)
                {
                    undo.Locked = false;
                    undo.Anim.Play(Fighter(undo).Animations[AnimationId.Stand], restart: true);
                    game.Audio.Play(Sfx.ShieldHit);
                }
                return;
            }
            if (target.Locked)
                return;
            if (keys.Pressed(InputButtons.Left)) MoveCursor(target, -1);
            if (keys.Pressed(InputButtons.Right)) MoveCursor(target, 1);
            if (keys.Pressed(InputButtons.Up)) ChangePalette(target, 1);
            if (keys.Pressed(InputButtons.Down)) ChangePalette(target, -1);
            if (keys.Pressed(InputButtons.Attack)) Lock(target);
        }

        void MoveCursor(Picker p, int dir)
        {
            int count = game.Characters.Count;
            p.Cursor = (p.Cursor + dir + count) % count;
            p.Anim.Play(Fighter(p).Animations[AnimationId.Stand], restart: true);
            game.Audio.Play(Sfx.MenuMove);
        }

        void ChangePalette(Picker p, int dir)
        {
            p.Palette = (p.Palette + dir + Palette.Count) % Palette.Count;
            game.Audio.Play(Sfx.MenuMove);
        }

        void Lock(Picker p)
        {
            //two players with the same fighter and colour would look identical
            while (pickers.Any(o => o != p && o.Locked && o.Cursor == p.Cursor && o.Palette == p.Palette))
                p.Palette = (p.Palette + 1) % Palette.Count;
            p.Locked = true;
            //a victory-style flourish: the fighter shows off an attack
            p.Anim.Play(Fighter(p).Animations[AnimationId.UpSmash], restart: true);
            game.Audio.Play(Sfx.MenuSelect);
            game.Audio.Play(Sfx.HitLight, 0.4f);
        }

        public override void Tick()
        {
            ticks++;
            foreach (Picker p in pickers)
            {
                p.Anim.Update();
                if (p.Anim.Finished)
                    p.Anim.Play(Fighter(p).Animations[AnimationId.Stand], restart: true);
            }

            if (readyTicks >= 0)
            {
                if (--readyTicks <= 0)
                    StartMatch();
                return;
            }
            if (timer > 0 && --timer == 0)
            {
                //time is up: everybody takes the fighter under their cursor
                foreach (Picker p in pickers.Where(p => !p.Locked))
                    Lock(p);
            }
            if (pickers.All(p => p.Locked))
            {
                readyTicks = ReadyDelay;
                game.Audio.Play(Sfx.Go);
            }
        }

        void StartMatch()
        {
            foreach (Picker p in pickers)
            {
                SlotSettings s = game.MatchSettings.Slots[p.Slot];
                s.Character = p.Cursor;
                s.Palette = p.Palette;
            }
            game.ChangeScene(new MatchScene(game));
        }

        #region draw
        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackground(sb);
            DrawTitle(sb);
            DrawPreviews(sb);
            DrawPortraits(sb);
            DrawHints(sb);
            if (readyTicks >= 0)
            {
                float s = 1 + Math.Max(0, readyTicks - ReadyDelay + 12) * 0.08f;
                game.Prims.Rect(sb, new Rectangle(0, (int)(Height * 0.42f), Width, 90), Color.Black * 0.7f);
                game.Font.DrawCentered(sb, "GET READY!", new Vector2(Width / 2f, Height * 0.42f + 45), new Color(255, 70, 40), 10 * s);
            }
            sb.End();
        }

        //dark backdrop with slanted red and orange stripes sliding past, and black cinematic bars
        void DrawBackground(SpriteBatch sb)
        {
            Texture2D px = game.Prims.Pixel;
            game.Prims.Rect(sb, new Rectangle(0, 0, Width, Height), new Color(12, 8, 14));
            DrawBackdrop(sb, 0.82f);
            float angle = MathHelper.ToRadians(-20);
            for (int i = 0; i < 14; i++)
            {
                float speed = 1.5f + i % 4;
                float x = (i * 173 + ticks * speed) % (Width + 600) - 300;
                Color c = (i % 3 == 0 ? new Color(220, 40, 30) : i % 3 == 1 ? new Color(255, 140, 20) : new Color(120, 10, 20)) * 0.13f;
                sb.Draw(px, new Vector2(x, Height + 50), null, c, angle - MathHelper.PiOver2 + 0.35f, Vector2.Zero, new Vector2(Height * 1.6f, 18 + i % 5 * 14), SpriteEffects.None, 0);
            }
            //a glowing floor line
            game.Prims.Rect(sb, new Rectangle(0, (int)(Height * 0.58f), Width, 3), new Color(255, 90, 40) * 0.5f);
            game.Prims.Rect(sb, new Rectangle(0, 0, Width, 26), Color.Black);
            game.Prims.Rect(sb, new Rectangle(0, Height - 26, Width, 26), Color.Black);
        }

        void DrawTitle(SpriteBatch sb)
        {
            PixelFont font = game.Font;
            Vector2 pos = new Vector2(Width / 2f, 70);
            //a heavy red shadow under white letters, like an arcade title
            font.DrawCentered(sb, "SELECT YOUR FIGHTER", pos + new Vector2(5, 5), new Color(170, 20, 20), 6, shadow: false);
            font.DrawCentered(sb, "SELECT YOUR FIGHTER", pos, Color.White, 6, shadow: false);

            int seconds = (timer + G.TicksPerSecond - 1) / G.TicksPerSecond;
            var box = new Rectangle(Width / 2 - 46, 108, 92, 58);
            game.Prims.Rect(sb, box, Color.Black * 0.75f);
            game.Prims.RectOutline(sb, box, new Color(255, 170, 40), 3);
            Color timeColor = seconds <= 5 && ticks / 8 % 2 == 0 ? new Color(255, 60, 40) : new Color(255, 210, 60);
            font.DrawCentered(sb, seconds.ToString("00"), box.Center.ToVector2(), timeColor, 5);

            Picker cpu = CurrentCpu;
            if (cpu != null && CpuController != null && readyTicks < 0)
                font.DrawCentered(sb, $"P{CpuController.Slot + 1} CHOOSES FOR CPU{cpu.Slot + 1}", new Vector2(Width / 2f, 186), MatchSettings.TagColors[cpu.Slot], 3);
        }

        //big fighters standing on the floor line, player colours underneath
        void DrawPreviews(SpriteBatch sb)
        {
            int n = pickers.Count;
            float height = n <= 2 ? 250 : 190;
            float floor = Height * 0.58f;
            for (int i = 0; i < n; i++)
            {
                Picker p = pickers[i];
                float fx = n == 2 ? (i == 0 ? 0.2f : 0.8f) : (i + 0.5f) / n * 0.9f + 0.05f;
                var pos = new Vector2(Width * fx, floor);
                bool faceLeft = fx > 0.5f;
                Character c = Fighter(p);
                Color tag = MatchSettings.TagColors[p.Slot];

                //spotlight and shadow
                game.Prims.Circle(sb, pos - new Vector2(0, height * 0.45f), height * 0.55f, tag * 0.12f);
                sb.Draw(game.Prims.Disc, pos, null, Color.Black * 0.5f, 0, new Vector2(64), new Vector2(height * 0.5f / 64, height * 0.06f / 64), SpriteEffects.None, 0);

                SheetData data = p.Anim.Current.Data;
                int frame = p.Anim.SheetFrame;
                Vector2 origin = data.Sheet.Origins[frame];
                Rectangle src = data.Sheet.Frames[frame];
                if (faceLeft)
                    origin.X = src.Width - origin.X;
                sb.Draw(data.Texture(game.GraphicsDevice, p.Palette), pos, src, Color.White, 0, origin, c.ScaleForHeight(height, 6.5f),
                    faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

                //name plate
                PixelFont font = game.Font;
                string who = (p.Cpu ? "CPU" : "P") + (p.Slot + 1);
                font.DrawCentered(sb, who, pos + new Vector2(0, 26), tag, 3);
                font.DrawCentered(sb, c.Name.ToUpperInvariant(), pos + new Vector2(0, 62), Color.White, n <= 2 ? 6 : 4);
                font.DrawCentered(sb, "< " + Palette.Names[p.Palette] + " >", pos + new Vector2(0, 100), new Color(255, 200, 80), 2);
                if (p.Locked)
                {
                    float wobble = (float)Math.Sin(ticks * 0.2f) * 2;
                    font.DrawCentered(sb, "READY", pos - new Vector2(0, height + 26 + wobble), new Color(255, 70, 40), 5);
                }
            }
        }

        //one tile per character with the player cursors around it
        void DrawPortraits(SpriteBatch sb)
        {
            int count = game.Characters.Count;
            const int size = 118, gap = 16;
            int total = count * size + (count - 1) * gap;
            int x0 = (Width - total) / 2;
            int y = Height - 172;
            PixelFont font = game.Font;
            for (int i = 0; i < count; i++)
            {
                var r = new Rectangle(x0 + i * (size + gap), y, size, size);
                game.Prims.Rect(sb, r, new Color(30, 22, 30));
                game.Prims.Rect(sb, new Rectangle(r.X, r.Y, r.Width, r.Height / 2), Color.White * 0.05f);

                Character c = game.Characters[i];
                Animation stand = c.Animations[AnimationId.Stand];
                SpriteSheet sheet = stand.Data.Sheet;
                Rectangle src = sheet.Frames[stand.First];
                float scale = Math.Min(c.ScaleForHeight(size - 26), (size - 12f) / src.Width);
                sb.Draw(stand.Data.Texture(game.GraphicsDevice, 0), new Vector2(r.Center.X, r.Bottom - 20), src, Color.White, 0,
                    sheet.Origins[stand.First], scale, SpriteEffects.None, 0);
                game.Prims.Rect(sb, new Rectangle(r.X, r.Bottom - 20, r.Width, 20), Color.Black * 0.7f);
                font.DrawCentered(sb, c.Name.ToUpperInvariant(), new Vector2(r.Center.X, r.Bottom - 10), Color.White, 2);

                //cursors: every player on this tile gets a coloured frame, nested inside each other
                int k = 0;
                foreach (Picker p in pickers.Where(p => p.Cursor == i))
                {
                    bool active = !p.Locked && (!p.Cpu || p == CurrentCpu);
                    if (p.Cpu && p != CurrentCpu && !p.Locked)
                        continue;
                    Color tag = MatchSettings.TagColors[p.Slot];
                    if (active && ticks / 10 % 2 == 0)
                        tag *= 0.6f;
                    var frame = new Rectangle(r.X - 4 + k * 4, r.Y - 4 + k * 4, r.Width + 8 - k * 8, r.Height + 8 - k * 8);
                    game.Prims.RectOutline(sb, frame, tag, 4);
                    string label = (p.Cpu ? "C" : "") + (p.Slot + 1) + "P";
                    Vector2 corner = k % 2 == 0 ? new Vector2(frame.X, frame.Y - 18) : new Vector2(frame.Right - PixelFont.Measure(label, 2).X, frame.Y - 18);
                    game.Prims.Rect(sb, new Rectangle((int)corner.X - 2, (int)corner.Y - 2, (int)PixelFont.Measure(label, 2).X + 4, 18), tag);
                    font.Draw(sb, label, corner, Color.Black, 2, shadow: false);
                    k++;
                }
            }
        }

        void DrawHints(SpriteBatch sb)
        {
            game.Font.DrawCentered(sb, "LEFT/RIGHT: FIGHTER    UP/DOWN: SKIN    ATTACK: SELECT    SHIELD: CANCEL    ESC: BACK",
                new Vector2(Width / 2f, Height - 13), Color.LightGray, 2, shadow: false);
        }
        #endregion
    }
}
