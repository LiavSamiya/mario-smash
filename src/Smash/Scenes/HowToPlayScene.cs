using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    class HowToPlayScene : Scene
    {
        static readonly string[] Controls =
        {
            "              KEYBOARD 1    KEYBOARD 2       GAMEPAD",
            "MOVE          A / D         LEFT / RIGHT     STICK / D-PAD",
            "JUMP / UP     W             UP               X / Y / STICK UP",
            "DOWN          S             DOWN             STICK DOWN",
            "ATTACK        F OR TAB      R-SHIFT OR K     A / B",
            "SHIELD        G OR Q        R-CTRL OR L      LB / RB / TRIGGERS",
        };

        static readonly string[] Moves =
        {
            "ATTACK ............. JAB   (PRESS AGAIN FOR A COMBO)",
            "ATTACK WHILE RUNNING  UPPERCUT",
            "DOWN + ATTACK ...... DOWN SMASH (HITS BOTH SIDES)",
            "UP + ATTACK ........ UP SMASH",
            "IN THE AIR: ATTACK = KICK, UP + ATTACK = UP AIR",
            "JUMP AGAIN IN THE AIR FOR A DOUBLE JUMP",
            "DOWN IN THE AIR .... FAST FALL",
            "SHIELD + LEFT/RIGHT  ROLL    SHIELD + DOWN  DODGE",
            "SHIELD IN THE AIR .. AIR DODGE",
            "ON A LEDGE: UP OR ATTACK TO CLIMB, DOWN TO LET GO",
        };

        public HowToPlayScene(Game1 game) : base(game) { }

        public override void Update()
        {
            if (game.Menu.Confirm() || game.Menu.Back())
            {
                game.Audio.Play(Sfx.MenuSelect);
                game.ChangeScene(new TitleScene(game));
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Ui);
            DrawBackdrop(sb, 0.7f);
            PixelFont font = game.Font;
            float x = Width / 2f - 480;
            font.DrawCentered(sb, "HOW TO PLAY", new Vector2(Width / 2f, 50), Color.Yellow, 6);
            font.Draw(sb, "KNOCK YOUR OPPONENTS OFF THE STAGE. THE MORE DAMAGE THEY HAVE, THE FURTHER THEY FLY.", new Vector2(x, 100), Color.White, 2);
            font.Draw(sb, "LOSE ALL YOUR STOCKS AND YOU ARE OUT. THE LAST ONE STANDING WINS!", new Vector2(x, 122), Color.White, 2);

            float y = 170;
            foreach (string line in Controls)
            {
                font.Draw(sb, line, new Vector2(x, y), line == Controls[0] ? Color.Yellow : Color.White, 2.5f);
                y += 26;
            }
            y += 20;
            foreach (string line in Moves)
            {
                font.Draw(sb, line, new Vector2(x, y), Color.White, 2.5f);
                y += 26;
            }
            y += 16;
            font.Draw(sb, "ESC / START: PAUSE    F3: SHOW HITBOXES    M: MUTE    F11: FULL SCREEN", new Vector2(x, y), Color.LightGray, 2);
            font.DrawCentered(sb, "PRESS ENTER", new Vector2(Width / 2f, Height - 30), Color.Yellow, 3);
            sb.End();
        }
    }
}
