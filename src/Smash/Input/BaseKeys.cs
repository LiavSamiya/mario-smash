using System;

namespace Smash
{
    //the logical buttons a fighter understands
    [Flags]
    enum InputButtons
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        Attack = 16,
        Shield = 32,
    }

    //abstract input source for a fighter. a fighter only talks to BaseKeys, so the same
    //character logic works for the keyboard (UserKeys), a gamepad (GamepadKeys) and the AI (BotKeys)
    abstract class BaseKeys
    {
        const int ButtonCount = 6;

        InputButtons current, previous;
        //how many ticks each button has been held for
        readonly int[] heldTicks = new int[ButtonCount];

        //short name shown in menus, for example "KEYBOARD 1"
        public abstract string Name { get; }

        //reads the raw state of the device
        protected abstract InputButtons Poll();

        //called once per simulation tick
        public void Update()
        {
            previous = current;
            current = Poll();
            for (int i = 0; i < ButtonCount; i++)
            {
                if ((current & (InputButtons)(1 << i)) != 0)
                    heldTicks[i]++;
                else
                    heldTicks[i] = 0;
            }
        }

        //forgets all state, so a button held during a menu does not count as a new press
        public void Reset()
        {
            current = previous = Poll();
            Array.Clear(heldTicks, 0, ButtonCount);
        }

        public bool Held(InputButtons b) => (current & b) != 0;
        public bool Pressed(InputButtons b) => (current & b) != 0 && (previous & b) == 0;
        public int HeldTicks(InputButtons b) => heldTicks[IndexOf(b)];

        public bool Left() => Held(InputButtons.Left);
        public bool Right() => Held(InputButtons.Right);
        public bool Up() => Held(InputButtons.Up);
        public bool Down() => Held(InputButtons.Down);
        public bool Attack() => Held(InputButtons.Attack);
        public bool Shield() => Held(InputButtons.Shield);

        //-1 = left, 1 = right, 0 = none or both
        public int Horizontal() => (Right() ? 1 : 0) - (Left() ? 1 : 0);

        static int IndexOf(InputButtons b)
        {
            int v = (int)b;
            for (int i = 0; i < ButtonCount; i++)
                if (v == 1 << i) return i;
            throw new ArgumentException("Expected a single button", nameof(b));
        }
    }
}
