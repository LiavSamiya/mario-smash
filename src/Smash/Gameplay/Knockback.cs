using System;
using Microsoft.Xna.Framework;

namespace Smash
{
    //knockback formula in the style of the Smash series: the more damage a fighter has, the further they fly
    static class Knockback
    {
        //world pixels per tick of launch speed for every unit of knockback
        public const float LaunchScale = 0.055f;
        //how much launch speed is lost every tick
        public const float LaunchDecay = 0.1f;
        //ticks of hitstun for every unit of knockback
        public const float HitstunScale = 0.4f;
        //the "Sakurai angle" launches at this angle when knockback is strong
        public const float SakuraiAngle = 40;
        const float SakuraiThreshold = 32;

        //percentAfter = the target's damage after the hit was added
        //every PercentScale percent of damage adds 100% more knockback (at 200% a hit launches twice as far as the formula alone)
        public const float PercentScale = 200;
        //the weight of an average sized fighter; heavier (bigger) fighters are launched less
        public const float ReferenceWeight = 100;

        //percentAfter = the target's damage after the hit was added
        public static float Compute(float percentAfter, float damage, float weight, float growth, float baseKnockback)
        {
            float p = percentAfter;
            float raw = ((p / 10f + p * damage / 20f) * 1.4f + 18f) * (growth / 100f) + baseKnockback;
            return raw * PercentMultiplier(p) * WeightMultiplier(weight);
        }

        //the more damage a fighter has, the more every hit launches them
        public static float PercentMultiplier(float percent) => 1 + Math.Max(0, percent) / PercentScale;

        //weight (set from the fighter's size) reduces knockback: 140 takes 71%, 90 takes 111%
        public static float WeightMultiplier(float weight) => ReferenceWeight / Math.Max(1, weight);

        public static float ResolveAngle(float angle, float knockback)
        {
            if (angle != 361)
                return angle;
            return knockback < SakuraiThreshold ? 0 : SakuraiAngle;
        }

        //launch velocity for a knockback value. direction = 1 to launch right, -1 to launch left
        public static Vector2 LaunchVelocity(float knockback, float angleDegrees, int direction)
        {
            float a = MathHelper.ToRadians(ResolveAngle(angleDegrees, knockback));
            float speed = knockback * LaunchScale;
            return new Vector2((float)Math.Cos(a) * speed * direction, -(float)Math.Sin(a) * speed);
        }

        public static int Hitstun(float knockback) => (int)(knockback * HitstunScale);

        //freeze frames for both fighters when a hit connects
        public static int Hitlag(float damage) => Math.Min(15, (int)(damage / 3f) + 3);
    }
}
