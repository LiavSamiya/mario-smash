using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Smash
{
    //checks every attack against every other fighter once per tick
    //  hitbox vs hurtbox -> the defender takes damage and knockback
    //  hitbox vs shield  -> the shield takes the damage
    //  hitbox vs hitbox  -> two ground attacks clash and both fighters rebound
    //  body vs body      -> grounded fighters are gently pushed apart
    class CombatSystem
    {
        readonly Match match;

        public CombatSystem(Match match)
        {
            this.match = match;
        }

        struct PendingHit
        {
            public Fighter Attacker, Defender;
            public HitData Hit;
            public Vector2 Point;
            public bool OnShield;
        }

        public void Resolve(List<Fighter> fighters)
        {
            ResolveClashes(fighters);

            //collect first and apply afterwards, so two fighters hitting each other on the same tick both connect
            var pending = new List<PendingHit>();
            foreach (Fighter attacker in fighters)
            {
                HitData hit = attacker.ActiveHit;
                if (hit == null)
                    continue;
                Circle[] hitCircles = attacker.HitCircles(hit).ToArray();
                foreach (Fighter defender in fighters)
                {
                    if (defender == attacker || defender.IsIntangible || defender.AlreadyHitBy(attacker, hit))
                        continue;
                    if (defender.IsShielding)
                    {
                        Circle shield = defender.ShieldCircle;
                        if (hitCircles.Any(c => c.Intersects(shield)))
                            pending.Add(new PendingHit { Attacker = attacker, Defender = defender, Hit = hit, Point = shield.Center, OnShield = true });
                        continue;
                    }
                    if (TryContact(hitCircles, defender.HurtCircles, out Vector2 point))
                        pending.Add(new PendingHit { Attacker = attacker, Defender = defender, Hit = hit, Point = point });
                }
            }
            foreach (PendingHit p in pending)
                Apply(p);

            PushApart(fighters);
        }

        void ResolveClashes(List<Fighter> fighters)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                for (int j = i + 1; j < fighters.Count; j++)
                {
                    Fighter a = fighters[i], b = fighters[j];
                    HitData ha = a.ActiveHit, hb = b.ActiveHit;
                    //like in Smash, only ground attacks clash; aerials trade hits
                    if (ha == null || hb == null || a.CurrentMove.Def.Aerial || b.CurrentMove.Def.Aerial)
                        continue;
                    if (!TryContact(a.HitCircles(ha), b.HitCircles(hb), out Vector2 point))
                        continue;
                    a.Rebound();
                    b.Rebound();
                    a.Hitlag = b.Hitlag = 6;
                    match.Effects.Spark(point, 20, Color.White);
                    match.Audio.Play(Sfx.Clank);
                }
            }
        }

        void Apply(PendingHit p)
        {
            Fighter a = p.Attacker, d = p.Defender;
            HitDefinition def = p.Hit.Def;
            d.RememberHit(a, p.Hit);

            //moves that hit on both sides push away from the attacker, everything else pushes forward
            HitboxDirection? shape = def.Hitbox?.Direction;
            int dir = a.FacingRight ? 1 : -1;
            if ((shape == HitboxDirection.Sides || shape == HitboxDirection.Around) && d.Position.X != a.Position.X)
                dir = Math.Sign(d.Position.X - a.Position.X);

            int lag = Knockback.Hitlag(def.Damage);
            if (p.OnShield)
            {
                d.ShieldHit(def.Damage, dir);
                a.Hitlag = d.Hitlag = lag / 2;
                match.Effects.Spark(p.Point, 18, d.TagColor);
                match.Audio.Play(Sfx.ShieldHit);
                return;
            }

            float knockback = Knockback.Compute(d.Damage + def.Damage, def.Damage, d.Character.Def.Weight, def.KnockbackGrowth, def.BaseKnockback);
            d.TakeHit(a, def.Damage, Knockback.LaunchVelocity(knockback, def.Angle, dir), Knockback.Hitstun(knockback));
            a.Hitlag = d.Hitlag = lag;
            match.Effects.Spark(p.Point, 14 + knockback / 6, knockback > 90 ? Color.Orange : Color.White);
            match.Audio.Play(knockback > 90 ? Sfx.HitHeavy : Sfx.HitLight);
            if (knockback > 110)
                match.Camera.Shake(Math.Min(12, knockback / 25));
        }

        static bool TryContact(IEnumerable<Circle> first, IEnumerable<Circle> second, out Vector2 point)
        {
            Circle[] others = second.ToArray();
            foreach (Circle a in first)
            {
                foreach (Circle b in others)
                {
                    if (a.Intersects(b))
                    {
                        point = (a.Center + b.Center) / 2;
                        return true;
                    }
                }
            }
            point = Vector2.Zero;
            return false;
        }

        //hurtbox against hurtbox: fighters standing inside each other slide apart instead of getting stuck
        static void PushApart(List<Fighter> fighters)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                for (int j = i + 1; j < fighters.Count; j++)
                {
                    Fighter a = fighters[i], b = fighters[j];
                    if (!a.Grounded || !b.Grounded || a.IsIntangible || b.IsIntangible)
                        continue;
                    float overlap = a.Character.BodyHalfWidth + b.Character.BodyHalfWidth - Math.Abs(a.Position.X - b.Position.X);
                    if (overlap <= 0)
                        continue;
                    float push = Math.Min(1.5f, overlap / 2);
                    float side = a.Position.X < b.Position.X ? -1 : a.Position.X > b.Position.X ? 1 : (a.Index < b.Index ? -1 : 1);
                    a.Position.X += side * push;
                    b.Position.X -= side * push;
                }
            }
        }
    }
}
