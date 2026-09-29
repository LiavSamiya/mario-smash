using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //short visual effects: hit sparks and knockout blasts
    class Effects
    {
        class Particle
        {
            public Vector2 Position;
            public float Size;
            public int Ticks;
            public int Life;
            public Color Color;
        }

        readonly List<Particle> particles = new List<Particle>();

        public void Spark(Vector2 position, float size, Color color) =>
            particles.Add(new Particle { Position = position, Size = size, Life = 12, Color = color });

        public void KoBlast(Vector2 position, Color color) =>
            particles.Add(new Particle { Position = position, Size = 220, Life = 40, Color = color });

        public void Update()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
                if (++particles[i].Ticks >= particles[i].Life)
                    particles.RemoveAt(i);
        }

        public void Draw(SpriteBatch sb, Primitives prims)
        {
            foreach (Particle p in particles)
            {
                float t = p.Ticks / (float)p.Life;
                float fade = 1 - t;
                prims.Circle(sb, p.Position, p.Size * (0.4f + t), p.Color * (0.5f * fade));
                prims.Circle(sb, p.Position, p.Size * (0.6f + 1.2f * t), p.Color * fade, filled: false);
            }
        }
    }
}
