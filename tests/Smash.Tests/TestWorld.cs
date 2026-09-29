using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace Smash.Tests
{
    //input whose buttons are set directly by the test
    class ScriptKeys : BaseKeys
    {
        public InputButtons Buttons;
        public override string Name => "SCRIPT";
        protected override InputButtons Poll() => Buttons;
    }

    //shared, headless game data (no window or graphics device needed)
    static class TestWorld
    {
        static readonly Lazy<List<Character>> characters = new Lazy<List<Character>>(Character.LoadAll);

        public static List<Character> Characters => characters.Value;
        public static Character Mario => Characters[0];

        public static Stage NewStage() => Stage.LoadDefault();

        //a two player match controlled by scripts, already past the countdown
        public static Match TwoPlayerMatch(out ScriptKeys p1, out ScriptKeys p2, int stocks = 3, int character1 = 0, int character2 = 0)
        {
            var settings = new MatchSettings { Stocks = stocks };
            settings.Slots[0].Character = character1;
            settings.Slots[1].Character = character2;
            p1 = new ScriptKeys();
            p2 = new ScriptKeys();
            return new Match(settings, Characters, NewStage(), null, skipCountdown: true, inputOverride: new BaseKeys[] { p1, p2 });
        }

        public static void Run(Match match, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                match.Tick();
        }

        //places both fighters on the stage facing each other at a given distance
        public static void FaceOff(Match match, float distance)
        {
            Fighter a = match.Fighters[0], b = match.Fighters[1];
            float x = match.Stage.Deck.Center.X;
            a.Position = new Vector2(x - distance / 2, match.Stage.Deck.Top);
            b.Position = new Vector2(x + distance / 2, match.Stage.Deck.Top);
            a.FacingRight = true;
            b.FacingRight = false;
        }
    }
}
