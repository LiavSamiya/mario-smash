using Microsoft.Xna.Framework;

namespace Smash
{
    //a circle used for hitboxes (deal damage) and hurtboxes (take damage)
    struct Circle
    {
        public Vector2 Center;
        public float Radius;

        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        //two circles touch when the distance between their centres is not bigger than the sum of their radii
        public bool Intersects(Circle other)
        {
            float r = Radius + other.Radius;
            return Vector2.DistanceSquared(Center, other.Center) <= r * r;
        }

        //converts a circle stored relative to a frame's origin into world space
        public Circle ToWorld(Vector2 position, float scale, bool facingRight)
        {
            Vector2 local = Center;
            if (!facingRight)
                local.X = -local.X;
            return new Circle(position + local * scale, Radius * scale);
        }
    }
}
