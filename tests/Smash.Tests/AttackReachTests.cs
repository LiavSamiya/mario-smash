using System.Collections.Generic;
using Xunit;

namespace Smash.Tests
{
    public class AttackReachTests
    {
        //the inputs that start each ground attack, fed on consecutive ticks
        static readonly Dictionary<string, InputButtons[]> Inputs = new Dictionary<string, InputButtons[]>
        {
            ["Jab"] = new[] { InputButtons.Attack },
            ["DownSmash"] = new[] { InputButtons.Down | InputButtons.Attack },
            ["UpSmash"] = new[] { InputButtons.Up, InputButtons.Up | InputButtons.Attack },
            ["UpTilt"] = new[] { InputButtons.Right, InputButtons.Right, InputButtons.Right, InputButtons.Right | InputButtons.Attack },
        };

        public static TheoryData<int, string> GroundAttacks()
        {
            var data = new TheoryData<int, string>();
            for (int c = 0; c < 4; c++)
                foreach (string move in Inputs.Keys)
                    data.Add(c, move);
            return data;
        }

        [Theory]
        [MemberData(nameof(GroundAttacks))]
        public void Ground_attacks_hit_an_adjacent_opponent(int character, string move)
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _, character1: character, character2: 0);
            Fighter attacker = match.Fighters[0];
            TestWorld.FaceOff(match, attacker.Character.BodyHalfWidth + match.Fighters[1].Character.BodyHalfWidth + 12);
            TestWorld.Run(match, 2);
            foreach (InputButtons b in Inputs[move])
            {
                p1.Buttons = b;
                TestWorld.Run(match, 1);
            }
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 90);

            Assert.True(match.Fighters[1].Damage > 0, $"{attacker.Character.Name} {move} missed");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Down_smash_hits_behind_too(int character)
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _, character1: character);
            Fighter attacker = match.Fighters[0];
            TestWorld.FaceOff(match, attacker.Character.BodyHalfWidth + match.Fighters[1].Character.BodyHalfWidth + 12);
            attacker.FacingRight = false;
            TestWorld.Run(match, 2);
            p1.Buttons = InputButtons.Down | InputButtons.Attack;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 90);

            Assert.True(match.Fighters[1].Damage > 0);
        }
    }
}
