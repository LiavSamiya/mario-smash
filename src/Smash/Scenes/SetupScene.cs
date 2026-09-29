using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //character select: choose who plays, with which controls, fighter and colour
    class SetupScene : Scene
    {
        class Field
        {
            public int Slot; //-1 = not part of a player card
            public Func<string> Label;
            public Func<bool> Enabled = () => true;
            public Action<int> Change;
        }

        readonly List<Field> fields = new List<Field>();
        readonly AnimationPlayer[] previews = new AnimationPlayer[G.MaxPlayers];
        int selected;
        string error;
        int errorTicks;

        MatchSettings Settings => game.MatchSettings;

        public SetupScene(Game1 game) : base(game)
        {
            for (int i = 0; i < G.MaxPlayers; i++)
            {
                SlotSettings s = Settings.Slots[i];
                fields.Add(new Field
                {
                    Slot = i,
                    Label = () => s.Type == SlotType.Off ? "OFF" : s.Type == SlotType.Human ? "HUMAN" : "CPU",
                    Change = d => s.Type = (SlotType)Wrap((int)s.Type + d, 3),
                });
                fields.Add(new Field
                {
                    Slot = i,
                    Label = () => s.Type == SlotType.Cpu ? "LEVEL " + s.CpuLevel : MatchSettings.DeviceName(s.Device),
                    Enabled = () => s.Type != SlotType.Off,
                    Change = d =>
                    {
                        if (s.Type == SlotType.Cpu) s.CpuLevel = Wrap(s.CpuLevel - 1 + d, 3) + 1;
                        else s.Device = Wrap(s.Device + d, MatchSettings.DeviceCount);
                    },
                });
                previews[i] = new AnimationPlayer();
                //start the preview now: the screen can be drawn before its first tick
                previews[i].Play(game.Characters[s.Character].Animations[AnimationId.Stand]);
            }
            fields.Add(new Field
            {
                Slot = -1,
                Label = () => "STOCKS: " + Settings.Stocks,
                Change = d => Settings.Stocks = Wrap(Settings.Stocks - 1 + d, MatchSettings.MaxStocks) + 1,
            });
            fields.Add(new Field { Slot = -1, Label = () => "CHOOSE FIGHTERS" });
            selected = fields.Count - 1;
        }

        static int Wrap(int value, int count) => ((value % count) + count) % count;

        public override void Update()
        {
            MenuInput input = game.Menu;
            if (input.Back())
            {
                game.ChangeScene(new TitleScene(game));
                return;
            }
            if (input.Up()) Move(-1);
            if (input.Down()) Move(1);

            Field f = fields[selected];
            int delta = input.Right() ? 1 : input.Left() ? -1 : 0;
            if (input.Confirm())
            {
                if (f.Change == null)
                {
                    TryStart();
                    return;
                }
                delta = 1;
            }
            if (delta != 0 && f.Change != null)
            {
                f.Change(delta);
                game.Audio.Play(Sfx.MenuMove);
            }
        }

        void Move(int dir)
        {
            do
            {
                selected = Wrap(selected + dir, fields.Count);
            } while (!fields[selected].Enabled());
            game.Audio.Play(Sfx.MenuMove);
        }

        void TryStart()
        {
            error = Validate();
            if (error != null)
            {
                errorTicks = 150;
                return;
            }
            game.Audio.Play(Sfx.MenuSelect);
            game.ChangeScene(new CharacterSelectScene(game));
        }

        string Validate()
        {
            if (Settings.ActiveCount < 2)
                return "AT LEAST TWO FIGHTERS ARE NEEDED";
            var devices = Settings.Slots.Where(s => s.Type == SlotType.Human).GroupBy(s => s.Device).FirstOrDefault(g => g.Count() > 1);
            if (devices != null)
                return "TWO PLAYERS CANNOT SHARE " + MatchSettings.DeviceName(devices.Key);
            return null;
        }

        public override void Tick()
        {
            if (errorTicks > 0) errorTicks--;
            for (int i = 0; i < G.MaxPlayers; i++)
            {
                previews[i].Play(game.Characters[Settings.Slots[i].Character].Animations[AnimationId.Stand]);
                previews[i].Update();
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackdrop(sb, 0.65f);
            PixelFont font = game.Font;
            font.DrawCentered(sb, "WHO IS PLAYING?", new Vector2(Width / 2f, 50), Color.Yellow, 6);

            const int cardW = 250, cardH = 400, gap = 20;
            int total = G.MaxPlayers * cardW + (G.MaxPlayers - 1) * gap;
            int x0 = (Width - total) / 2;
            int y0 = 100;
            for (int i = 0; i < G.MaxPlayers; i++)
                DrawCard(sb, i, new Rectangle(x0 + i * (cardW + gap), y0, cardW, cardH));

            float y = y0 + cardH + 40;
            foreach (Field f in fields.Where(f => f.Slot < 0))
            {
                bool sel = fields[selected] == f;
                font.DrawCentered(sb, sel ? "> " + f.Label() + " <" : f.Label(), new Vector2(Width / 2f, y), sel ? Color.Yellow : Color.White, 4);
                y += 46;
            }

            if (errorTicks > 0 && error != null)
                font.DrawCentered(sb, error, new Vector2(Width / 2f, y + 6), new Color(255, 90, 90), 3);
            font.DrawCentered(sb, "UP/DOWN: SELECT    LEFT/RIGHT: CHANGE    ENTER: CONFIRM    ESC: BACK",
                new Vector2(Width / 2f, Height - 42), Color.LightGray, 2);
            DrawDisclaimer(sb);
            sb.End();
        }

        void DrawCard(SpriteBatch sb, int slot, Rectangle r)
        {
            SlotSettings s = Settings.Slots[slot];
            Color tag = MatchSettings.TagColors[slot];
            bool on = s.Type != SlotType.Off;
            PixelFont font = game.Font;

            game.Prims.Rect(sb, r, Color.Black * (on ? 0.6f : 0.35f));
            game.Prims.Rect(sb, new Rectangle(r.X, r.Y, r.Width, 6), on ? tag : tag * 0.3f);
            font.DrawCentered(sb, "P" + (slot + 1), new Vector2(r.Center.X, r.Y + 30), on ? tag : Color.Gray, 5);

            if (on)
            {
                AnimationPlayer anim = previews[slot];
                SheetData data = anim.Current.Data;
                int frame = anim.SheetFrame;
                sb.Draw(data.Texture(game.GraphicsDevice, s.Palette), new Vector2(r.Center.X, r.Y + 210), data.Sheet.Frames[frame],
                    Color.White, 0, data.Sheet.Origins[frame], game.Characters[s.Character].ScaleForHeight(140), SpriteEffects.None, 0);
            }

            string[] titles = { "TYPE", s.Type == SlotType.Cpu ? "DIFFICULTY" : "CONTROLS" };
            var cardFields = fields.Where(f => f.Slot == slot).ToList();
            float y = r.Y + 232;
            for (int i = 0; i < cardFields.Count; i++)
            {
                Field f = cardFields[i];
                bool sel = fields[selected] == f;
                bool enabled = f.Enabled();
                font.DrawCentered(sb, titles[i], new Vector2(r.Center.X, y), Color.Gray, 2);
                string label = enabled ? f.Label() : "-";
                if (sel) label = "< " + label + " >";
                font.DrawCentered(sb, label, new Vector2(r.Center.X, y + 20), sel ? Color.Yellow : enabled ? Color.White : Color.DimGray, 2.5f);
                y += 42;
            }
        }
    }
}
