using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Xunit;
using Xunit.Abstractions;

namespace Smash.Tests
{
    public class MechanicsTests
    {
        readonly ITestOutputHelper output;

        public MechanicsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Circles_touch_when_centres_are_closer_than_the_sum_of_radii()
        {
            var a = new Circle(Vector2.Zero, 5);
            Assert.True(a.Intersects(new Circle(new Vector2(9, 0), 4)));
            Assert.False(a.Intersects(new Circle(new Vector2(9.5f, 0), 4)));
        }

        [Fact]
        public void Circles_are_mirrored_when_facing_left()
        {
            var local = new Circle(new Vector2(10, -20), 3);
            Circle right = local.ToWorld(new Vector2(100, 100), 2, facingRight: true);
            Circle left = local.ToWorld(new Vector2(100, 100), 2, facingRight: false);
            Assert.Equal(new Vector2(120, 60), right.Center);
            Assert.Equal(new Vector2(80, 60), left.Center);
            Assert.Equal(6, right.Radius);
        }

        [Fact]
        public void Knockback_grows_with_damage_and_is_smaller_for_heavier_fighters()
        {
            float low = Knockback.Compute(10, 10, 100, 100, 30);
            float high = Knockback.Compute(120, 10, 100, 100, 30);
            float heavy = Knockback.Compute(120, 10, 140, 100, 30);
            Assert.True(high > low);
            Assert.True(heavy < high);
        }

        [Fact]
        public void Jab_hits_once_and_adds_its_damage()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            TestWorld.FaceOff(match, 40);
            TestWorld.Run(match, 2);

            p1.Buttons = InputButtons.Attack;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 60);

            Fighter target = match.Fighters[1];
            Assert.Equal(3, target.Damage);
            Assert.Equal(3, match.Fighters[0].Stats.DamageDealt);
        }

        [Fact]
        public void Shield_blocks_damage()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out ScriptKeys p2);
            TestWorld.FaceOff(match, 40);
            p2.Buttons = InputButtons.Shield;
            TestWorld.Run(match, 3);

            p1.Buttons = InputButtons.Attack;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 40);

            Fighter target = match.Fighters[1];
            Assert.Equal(0, target.Damage);
            Assert.True(target.ShieldHealth < Fighter.MaxShield - 3);
        }

        [Fact]
        public void A_strong_up_smash_at_high_percent_knocks_out()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            TestWorld.FaceOff(match, 30);
            Fighter target = match.Fighters[1];
            target.Damage = 150;
            TestWorld.Run(match, 2);

            //up then attack during the jump squat = up smash
            p1.Buttons = InputButtons.Up;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.Up | InputButtons.Attack;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 300);

            Assert.Equal(2, target.Stocks);
            Assert.Equal(1, match.Fighters[0].Stats.KOs);
        }

        [Fact]
        public void Knockout_percent_is_reasonable()
        {
            //find the lowest percent at which the down smash kills from the middle of the stage
            int killPercent = -1;
            for (int percent = 0; percent <= 300 && killPercent < 0; percent += 10)
            {
                Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
                TestWorld.FaceOff(match, 30);
                match.Fighters[1].Damage = percent;
                TestWorld.Run(match, 2);
                p1.Buttons = InputButtons.Down | InputButtons.Attack;
                TestWorld.Run(match, 1);
                p1.Buttons = InputButtons.None;
                TestWorld.Run(match, 400);
                if (match.Fighters[1].Stocks < 3)
                    killPercent = percent;
            }
            output.WriteLine($"Down smash kills from the centre at {killPercent}%");
            Assert.InRange(killPercent, 60, 220);
        }

        [Fact]
        public void Knocked_out_fighters_respawn_without_leaking_event_handlers()
        {
            Match match = TestWorld.TwoPlayerMatch(out _, out _, stocks: 3);
            Fighter f = match.Fighters[0];
            int handlers = UpdateHandlerCount(match);

            for (int i = 0; i < 2; i++)
            {
                f.Position = new Vector2(match.Stage.BlastZone.Right + 50, 300);
                TestWorld.Run(match, 1);
                Assert.Equal(FighterState.Dead, f.State);
                TestWorld.Run(match, 400);
                Assert.NotEqual(FighterState.Dead, f.State);
            }

            Assert.Equal(1, f.Stocks);
            Assert.Equal(handlers, UpdateHandlerCount(match));
        }

        [Fact]
        public void Disposing_a_match_unsubscribes_every_fighter()
        {
            Match match = TestWorld.TwoPlayerMatch(out _, out _);
            match.Dispose();
            Assert.Equal(0, UpdateHandlerCount(match));
        }

        [Fact]
        public void Losing_the_last_stock_ends_the_match()
        {
            Match match = TestWorld.TwoPlayerMatch(out _, out _, stocks: 1);
            match.Fighters[1].Position = new Vector2(match.Stage.BlastZone.Left - 50, 300);
            TestWorld.Run(match, 1);

            Assert.True(match.Over);
            Assert.Same(match.Fighters[0], match.Winner);
        }

        [Fact]
        public void Fighters_cannot_walk_into_the_side_of_the_stage()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            Fighter f = match.Fighters[0];
            Box deck = match.Stage.Deck;
            //in the air beside the deck, level with it, knocked sideways into it
            //(a fighter in hitstun cannot grab the ledge, so this only tests the wall)
            f.Position = new Vector2(deck.Left - f.Character.BodyHalfWidth - 4, deck.Top + f.Character.BodyHeight + 10);
            f.Grounded = false;
            f.TakeHit(match.Fighters[1], 0, new Vector2(5, 0), 60);
            TestWorld.Run(match, 8);
            Assert.True(f.Body.Overlaps(new Box(deck.Left - 100, deck.Top, deck.Left, deck.Bottom)), "the fighter is no longer level with the deck");

            Assert.True(f.Position.X + f.Character.BodyHalfWidth <= deck.Left + 0.5f,
                $"fighter clipped into the stage: x={f.Position.X}, deck left={deck.Left}");
        }

        [Fact]
        public void Falling_next_to_the_ledge_grabs_it_and_lets_go_after_about_a_second()
        {
            Match match = TestWorld.TwoPlayerMatch(out _, out _);
            Fighter f = match.Fighters[0];
            Box deck = match.Stage.Deck;
            Vector2 tip = match.Stage.LedgeRight;
            f.Position = new Vector2(tip.X + f.Character.BodyHalfWidth + 5, tip.Y + f.Character.BodyHeight - 40);
            f.Grounded = false;
            TestWorld.Run(match, 30);
            Assert.Equal(FighterState.LedgeHang, f.State);

            //still hanging a little later
            TestWorld.Run(match, 20);
            Assert.Equal(FighterState.LedgeHang, f.State);

            //about one second after grabbing, the fighter falls
            TestWorld.Run(match, 50);
            Assert.NotEqual(FighterState.LedgeHang, f.State);
            Assert.False(f.Grounded);
            Assert.True(f.Position.X > deck.Right);
        }

        [Fact]
        public void The_ledge_is_only_grabbed_right_next_to_the_tip_of_the_stage()
        {
            Match match = TestWorld.TwoPlayerMatch(out _, out _);
            Fighter f = match.Fighters[0];
            Vector2 tip = match.Stage.LedgeRight;
            //falling 40 pixels away from the tip is too far to grab it
            f.Position = new Vector2(tip.X + f.Character.BodyHalfWidth + 40, tip.Y + f.Character.BodyHeight - 40);
            f.Grounded = false;
            TestWorld.Run(match, 40);

            Assert.NotEqual(FighterState.LedgeHang, f.State);
            Assert.True(f.Position.Y > tip.Y + f.Character.BodyHeight);
        }

        [Fact]
        public void Knockback_multipliers_follow_percent_and_weight()
        {
            Assert.Equal(1f, Knockback.PercentMultiplier(0));
            Assert.Equal(2f, Knockback.PercentMultiplier(Knockback.PercentScale));
            float[] weights = TestWorld.Characters.Select(c => c.Def.Weight).ToArray();
            //Mario, Bowser, Kratos, Sasuke: the biggest fighter (Bowser) is launched the least
            Assert.True(Knockback.WeightMultiplier(weights[1]) < Knockback.WeightMultiplier(weights[2]));
            Assert.True(Knockback.WeightMultiplier(weights[2]) < Knockback.WeightMultiplier(weights[3]));
            Assert.True(Knockback.WeightMultiplier(weights[3]) < Knockback.WeightMultiplier(weights[0]));
        }

        [Fact]
        public void Pressing_up_on_the_ledge_climbs_onto_the_stage()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            Fighter f = match.Fighters[0];
            Box deck = match.Stage.Deck;
            Vector2 tip = match.Stage.LedgeLeft;
            f.Position = new Vector2(tip.X - f.Character.BodyHalfWidth - 5, tip.Y + f.Character.BodyHeight - 40);
            f.Grounded = false;
            TestWorld.Run(match, 30);
            Assert.Equal(FighterState.LedgeHang, f.State);

            p1.Buttons = InputButtons.Up;
            TestWorld.Run(match, 2);
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 20);

            Assert.True(f.Grounded);
            Assert.InRange(f.Position.X, deck.Left, deck.Right);
        }

        [Fact]
        public void Directional_air_dodge_moves_the_fighter_up()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            Fighter f = match.Fighters[0];
            f.Position = new Vector2(match.Stage.Deck.Left - 200, match.Stage.Deck.Top + 200);
            f.Grounded = false;
            TestWorld.Run(match, 1);
            float y = f.Position.Y;
            p1.Buttons = InputButtons.Up | InputButtons.Right | InputButtons.Shield;
            TestWorld.Run(match, 10);

            Assert.Equal(FighterState.AirDodge, f.State);
            Assert.True(f.Position.Y < y - 30, $"moved from {y} to {f.Position.Y}");
        }

        [Fact]
        public void Hurtboxes_do_not_change_while_animating()
        {
            //regression: the old updateCircles() scaled the stored circles every frame so they grew forever
            Match match = TestWorld.TwoPlayerMatch(out _, out _);
            Fighter f = match.Fighters[0];
            TestWorld.Run(match, 2);
            float before = f.HurtCircles.Sum(c => c.Radius);
            Animation stand = f.Anim.Current;
            TestWorld.Run(match, stand.Count * stand.FrameTicks); // one full loop of the standing animation
            float after = f.HurtCircles.Sum(c => c.Radius);
            Assert.Equal(before, after, 3);
        }

        [Fact]
        public void Double_jump_is_available_once_per_airtime()
        {
            Match match = TestWorld.TwoPlayerMatch(out ScriptKeys p1, out _);
            Fighter f = match.Fighters[0];
            p1.Buttons = InputButtons.Up;
            TestWorld.Run(match, 12);
            Assert.False(f.Grounded);
            float peakBefore = f.Position.Y;

            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.Up;
            TestWorld.Run(match, 1);
            Assert.True(f.UsedDoubleJump);
            Assert.True(f.Velocity.Y < 0);

            //a third press does nothing
            p1.Buttons = InputButtons.None;
            TestWorld.Run(match, 1);
            p1.Buttons = InputButtons.Up;
            float vy = f.Velocity.Y;
            TestWorld.Run(match, 1);
            Assert.True(f.Velocity.Y >= vy);
            Assert.True(f.Position.Y < peakBefore);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Cpu_matches_finish(int level)
        {
            var settings = new MatchSettings { Stocks = 2 };
            settings.Slots[0].Type = SlotType.Cpu;
            settings.Slots[1].Type = SlotType.Cpu;
            settings.Slots[0].CpuLevel = level;
            settings.Slots[1].CpuLevel = level;
            var match = new Match(settings, TestWorld.Characters, TestWorld.NewStage(), null, seed: 7);

            int ticks = 0;
            const int limit = 10 * 60 * G.TicksPerSecond;
            while (!match.Over && ticks < limit)
            {
                match.Tick();
                ticks++;
            }
            output.WriteLine($"CPU level {level} match took {ticks / (float)G.TicksPerSecond:0}s, " +
                string.Join(", ", match.Fighters.Select(f => $"{f.Tag}{f.Index + 1}: {f.Stats.KOs} KOs, {f.Stats.SelfDestructs} SDs, dealt {f.Stats.DamageDealt}%")));
            Assert.True(match.Over, "the match did not finish within 10 minutes");
            Assert.True(match.Fighters.Sum(f => f.Stats.DamageDealt) > 0);
        }

        static int UpdateHandlerCount(Match match)
        {
            var field = typeof(Match).GetField("event_update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var del = (Delegate)field.GetValue(match);
            return del?.GetInvocationList().Length ?? 0;
        }
    }
}
