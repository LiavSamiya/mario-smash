using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Smash
{
    //decides which buttons a computer controlled fighter presses. it plans a few ticks of input at a time,
    //waits a reaction delay between decisions and makes more mistakes on lower levels
    class AiBrain
    {
        public int Level { get; }

        readonly Random rng;
        readonly Queue<InputButtons> script = new Queue<InputButtons>();
        Fighter self;
        Match match;

        public AiBrain(int level, int seed)
        {
            Level = Math.Clamp(level, 1, 3);
            rng = new Random(seed);
        }

        public void Attach(Fighter fighter, Match match)
        {
            self = fighter;
            this.match = match;
        }

        int ReactionTicks => Level switch { 1 => 22, 2 => 12, _ => 5 };
        double ShieldChance => Level switch { 1 => 0.05, 2 => 0.2, _ => 0.45 };
        double MistakeChance => Level switch { 1 => 0.25, 2 => 0.1, _ => 0.02 };

        public InputButtons Think()
        {
            if (self == null || !match.Started || match.Over)
                return InputButtons.None;
            if (script.Count == 0)
                Decide();
            return script.Count > 0 ? script.Dequeue() : InputButtons.None;
        }

        void Wait(int ticks, InputButtons held = InputButtons.None)
        {
            for (int i = 0; i < ticks; i++)
                script.Enqueue(held);
        }

        void Decide()
        {
            Box deck = match.Stage.Deck;
            switch (self.State)
            {
                case FighterState.Dead:
                    Wait(10);
                    return;
                case FighterState.LedgeHang:
                    //climb back up after a short moment
                    Wait(6 + rng.Next(20));
                    script.Enqueue(InputButtons.Up);
                    return;
                case FighterState.Respawn:
                    //drop from the respawn platform after a moment
                    Wait(20 + rng.Next(30));
                    script.Enqueue(InputButtons.Down);
                    return;
                case FighterState.Hitstun:
                case FighterState.ShieldBreak:
                case FighterState.JumpSquat:
                case FighterState.Landing:
                case FighterState.Roll:
                case FighterState.SpotDodge:
                    Wait(1);
                    return;
            }

            bool offStage = self.Position.X < deck.Left || self.Position.X > deck.Right || self.Position.Y > deck.Top + 2;
            if (!self.Grounded && offStage)
            {
                Recover(deck);
                return;
            }

            if (rng.NextDouble() < MistakeChance)
            {
                Wait(ReactionTicks);
                return;
            }

            Fighter target = NearestOpponent();
            if (target == null)
            {
                Wait(ReactionTicks);
                return;
            }

            Vector2 d = target.Position - self.Position;
            float adx = Math.Abs(d.X);
            InputButtons toward = d.X >= 0 ? InputButtons.Right : InputButtons.Left;
            bool facingTarget = (d.X >= 0) == self.FacingRight;

            //shield when an attack is coming
            if (self.Grounded && target.State == FighterState.Attack && adx < 130 && Math.Abs(d.Y) < 100 && rng.NextDouble() < ShieldChance)
            {
                Wait(12 + rng.Next(10), InputButtons.Shield);
                return;
            }

            if (self.Grounded)
            {
                if (self.State == FighterState.Attack || self.State == FighterState.Shield)
                {
                    Wait(1);
                    return;
                }
                //close and level: attack
                if (adx < 60 && d.Y > -45 && d.Y < 35)
                {
                    if (!facingTarget)
                        script.Enqueue(toward);
                    double r = rng.NextDouble();
                    if (r < 0.3)
                    {
                        script.Enqueue(InputButtons.Down | InputButtons.Attack);
                    }
                    else
                    {
                        script.Enqueue(InputButtons.Attack);
                        Wait(3);
                        if (r < 0.7)
                            script.Enqueue(InputButtons.Attack);
                    }
                    Wait(ReactionTicks);
                    return;
                }
                //target above: up smash or jump and up air
                if (adx < 70 && d.Y < -45 && d.Y > -220)
                {
                    if (rng.NextDouble() < 0.5)
                    {
                        script.Enqueue(InputButtons.Up);
                        script.Enqueue(InputButtons.Up | InputButtons.Attack);
                    }
                    else
                    {
                        Wait(10, InputButtons.Up);
                        script.Enqueue(InputButtons.Attack | InputButtons.Up);
                    }
                    Wait(ReactionTicks);
                    return;
                }
                //do not follow a target off the stage
                bool targetOff = target.Position.X < deck.Left || target.Position.X > deck.Right;
                float edge = toward == InputButtons.Right ? deck.Right - self.Position.X : self.Position.X - deck.Left;
                if (targetOff && edge < 80)
                {
                    Wait(ReactionTicks);
                    return;
                }
                //jump towards targets that are far above
                if (d.Y < -120 && adx < 260 && rng.NextDouble() < 0.4)
                {
                    Wait(12, InputButtons.Up | toward);
                    return;
                }
                Wait(Math.Max(4, ReactionTicks / 2), toward);
                return;
            }

            //in the air above the stage
            if (Vector2.Distance(self.Position, target.Position) < 80 && self.State == FighterState.Airborne)
            {
                script.Enqueue(d.Y < -30 ? InputButtons.Up | InputButtons.Attack : InputButtons.Attack | toward);
                Wait(ReactionTicks / 2, toward);
                return;
            }
            if (d.Y > 80 && self.Velocity.Y > 0 && rng.NextDouble() < 0.3)
                script.Enqueue(InputButtons.Down | toward);
            Wait(Math.Max(3, ReactionTicks / 2), toward);
        }

        //get back to the stage: drift towards the middle and double jump when falling below the surface
        void Recover(Box deck)
        {
            InputButtons toward = self.Position.X < deck.Center.X ? InputButtons.Right : InputButtons.Left;
            if (self.State == FighterState.Attack || self.State == FighterState.AirDodge)
            {
                script.Enqueue(toward);
                return;
            }
            if (!self.UsedDoubleJump && self.Velocity.Y > 0 && self.Position.Y > deck.Top - 80)
            {
                script.Enqueue(toward | InputButtons.Up);
                Wait(2, toward);
                return;
            }
            //out of jumps and still falling below the stage: air dodge up towards it
            if (self.UsedDoubleJump && !self.UsedAirDodge && self.Velocity.Y > 1 && self.Position.Y > deck.Top + 10)
            {
                script.Enqueue(toward | InputButtons.Up | InputButtons.Shield);
                Wait(2, toward);
                return;
            }
            script.Enqueue(toward);
        }

        Fighter NearestOpponent()
        {
            Fighter best = null;
            float bestDist = float.MaxValue;
            foreach (Fighter f in match.Fighters)
            {
                if (f == self || f.State == FighterState.Dead || f.State == FighterState.Respawn)
                    continue;
                float dist = Vector2.DistanceSquared(f.Position, self.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = f;
                }
            }
            return best;
        }
    }
}
