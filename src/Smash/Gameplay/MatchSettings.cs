using Microsoft.Xna.Framework;

namespace Smash
{
    enum SlotType
    {
        Off,
        Human,
        Cpu,
    }

    //the choices made on the setup screen for one player
    class SlotSettings
    {
        public SlotType Type;
        //0 = keyboard 1, 1 = keyboard 2, 2..5 = gamepad 1..4
        public int Device;
        public int Character;
        public int Palette;
        //1 (easy) to 3 (hard)
        public int CpuLevel = 2;
    }

    class MatchSettings
    {
        public const int DeviceCount = 6;
        public const int MaxStocks = 9;

        public SlotSettings[] Slots { get; } = new SlotSettings[G.MaxPlayers];
        public int Stocks = 3;

        public static readonly Color[] TagColors =
        {
            new Color(255, 70, 70),
            new Color(70, 140, 255),
            new Color(255, 210, 40),
            new Color(60, 210, 90),
        };

        public MatchSettings()
        {
            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = new SlotSettings { Type = SlotType.Off, Device = i, Palette = i };
            Slots[0].Type = SlotType.Human;
            Slots[1].Type = SlotType.Human;
        }

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (SlotSettings s in Slots)
                    if (s.Type != SlotType.Off) n++;
                return n;
            }
        }

        public static string DeviceName(int device) => device switch
        {
            0 => "KEYBOARD 1",
            1 => "KEYBOARD 2",
            _ => "GAMEPAD " + (device - 1),
        };

        public static BaseKeys CreateKeys(int device) => device switch
        {
            0 => UserKeys.Keyboard1(),
            1 => UserKeys.Keyboard2(),
            _ => new GamepadKeys(device - 2),
        };
    }
}
