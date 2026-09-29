using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Smash
{
    //menu navigation from the keyboard and every connected gamepad. updated once per rendered frame
    class MenuInput
    {
        KeyboardState currKey, prvKey;
        readonly GamePadState[] currPad = new GamePadState[G.MaxPlayers];
        readonly GamePadState[] prvPad = new GamePadState[G.MaxPlayers];

        public void Update()
        {
            prvKey = currKey;
            currKey = Keyboard.GetState();
            for (int i = 0; i < G.MaxPlayers; i++)
            {
                prvPad[i] = currPad[i];
                currPad[i] = GamePad.GetState((PlayerIndex)i);
            }
        }

        public bool KeyPressed(Keys k) => currKey.IsKeyDown(k) && prvKey.IsKeyUp(k);

        bool PadPressed(Buttons b)
        {
            for (int i = 0; i < G.MaxPlayers; i++)
                if (currPad[i].IsButtonDown(b) && prvPad[i].IsButtonUp(b))
                    return true;
            return false;
        }

        public bool Up() => KeyPressed(Keys.Up) || KeyPressed(Keys.W) || PadPressed(Buttons.DPadUp) || PadPressed(Buttons.LeftThumbstickUp);
        public bool Down() => KeyPressed(Keys.Down) || KeyPressed(Keys.S) || PadPressed(Buttons.DPadDown) || PadPressed(Buttons.LeftThumbstickDown);
        public bool Left() => KeyPressed(Keys.Left) || KeyPressed(Keys.A) || PadPressed(Buttons.DPadLeft) || PadPressed(Buttons.LeftThumbstickLeft);
        public bool Right() => KeyPressed(Keys.Right) || KeyPressed(Keys.D) || PadPressed(Buttons.DPadRight) || PadPressed(Buttons.LeftThumbstickRight);
        public bool Confirm() => KeyPressed(Keys.Enter) || KeyPressed(Keys.Space) || PadPressed(Buttons.A) || PadPressed(Buttons.Start);
        public bool Back() => KeyPressed(Keys.Escape) || KeyPressed(Keys.Back) || PadPressed(Buttons.B) || PadPressed(Buttons.Back);
        public bool Pause() => KeyPressed(Keys.Escape) || KeyPressed(Keys.P) || PadPressed(Buttons.Start);
    }
}
