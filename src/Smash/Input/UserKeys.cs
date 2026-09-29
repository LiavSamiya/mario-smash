using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Smash
{
    //keyboard controls. every action can be bound to several keys
    class UserKeys : BaseKeys
    {
        readonly string name;
        readonly Keys[] left, right, up, down, attack, shield;

        public UserKeys(string name, Keys[] left, Keys[] right, Keys[] up, Keys[] down, Keys[] attack, Keys[] shield)
        {
            this.name = name;
            this.left = left;
            this.right = right;
            this.up = up;
            this.down = down;
            this.attack = attack;
            this.shield = shield;
        }

        public override string Name => name;

        //W A S D, attack F (or Tab), shield G (or Q)
        public static UserKeys Keyboard1() => new UserKeys("KEYBOARD 1",
            new[] { Keys.A }, new[] { Keys.D }, new[] { Keys.W }, new[] { Keys.S },
            new[] { Keys.F, Keys.Tab }, new[] { Keys.G, Keys.Q });

        //arrow keys, attack Right Shift (or K), shield Right Ctrl (or L / Numpad 0)
        public static UserKeys Keyboard2() => new UserKeys("KEYBOARD 2",
            new[] { Keys.Left }, new[] { Keys.Right }, new[] { Keys.Up }, new[] { Keys.Down },
            new[] { Keys.RightShift, Keys.K }, new[] { Keys.RightControl, Keys.L, Keys.NumPad0 });

        protected override InputButtons Poll()
        {
            KeyboardState ks = Keyboard.GetState();
            InputButtons b = InputButtons.None;
            if (left.Any(ks.IsKeyDown)) b |= InputButtons.Left;
            if (right.Any(ks.IsKeyDown)) b |= InputButtons.Right;
            if (up.Any(ks.IsKeyDown)) b |= InputButtons.Up;
            if (down.Any(ks.IsKeyDown)) b |= InputButtons.Down;
            if (attack.Any(ks.IsKeyDown)) b |= InputButtons.Attack;
            if (shield.Any(ks.IsKeyDown)) b |= InputButtons.Shield;
            return b;
        }
    }
}
