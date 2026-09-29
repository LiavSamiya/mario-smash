using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Smash
{
    //gamepad controls: left stick / d-pad to move, A or B to attack,
    //X or Y (or stick up) to jump, bumpers or triggers to shield
    class GamepadKeys : BaseKeys
    {
        const float StickThreshold = 0.5f;
        const float TriggerThreshold = 0.4f;

        readonly int index;

        public GamepadKeys(int index)
        {
            this.index = index;
        }

        public override string Name => "GAMEPAD " + (index + 1);

        protected override InputButtons Poll()
        {
            GamePadState s = GamePad.GetState((PlayerIndex)index);
            if (!s.IsConnected)
                return InputButtons.None;

            Vector2 stick = s.ThumbSticks.Left;
            InputButtons b = InputButtons.None;
            if (stick.X < -StickThreshold || s.DPad.Left == ButtonState.Pressed) b |= InputButtons.Left;
            if (stick.X > StickThreshold || s.DPad.Right == ButtonState.Pressed) b |= InputButtons.Right;
            if (stick.Y > StickThreshold || s.DPad.Up == ButtonState.Pressed
                || s.IsButtonDown(Buttons.X) || s.IsButtonDown(Buttons.Y)) b |= InputButtons.Up;
            if (stick.Y < -StickThreshold || s.DPad.Down == ButtonState.Pressed) b |= InputButtons.Down;
            if (s.IsButtonDown(Buttons.A) || s.IsButtonDown(Buttons.B)) b |= InputButtons.Attack;
            if (s.IsButtonDown(Buttons.LeftShoulder) || s.IsButtonDown(Buttons.RightShoulder)
                || s.Triggers.Left > TriggerThreshold || s.Triggers.Right > TriggerThreshold) b |= InputButtons.Shield;
            return b;
        }
    }
}
