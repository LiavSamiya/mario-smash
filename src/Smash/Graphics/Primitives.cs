using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //generated textures for simple shapes (rectangles, circles, rings)
    class Primitives
    {
        public Texture2D Pixel { get; }
        public Texture2D Disc { get; }
        public Texture2D Ring { get; }
        //an energy bubble with a hexagon pattern (the shield)
        public Texture2D ShieldBubble { get; }

        const int Size = 128;

        public Primitives(GraphicsDevice gd)
        {
            Pixel = new Texture2D(gd, 1, 1);
            Pixel.SetData(new[] { Color.White });
            Disc = MakeCircle(gd, filled: true);
            Ring = MakeCircle(gd, filled: false);
            ShieldBubble = MakeShield(gd);
        }

        static Texture2D MakeShield(GraphicsDevice gd)
        {
            var data = new Color[Size * Size];
            float r = Size / 2f - 1;
            const float spacing = 14;
            //three families of lines 60 degrees apart draw a hexagon/triangle lattice
            var normals = new[] { new Vector2(1, 0), new Vector2(0.5f, 0.866f), new Vector2(-0.5f, 0.866f) };
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(Size / 2f);
                    float d = p.Length() / r;
                    if (d > 1) continue;
                    //thin in the middle, glowing towards the edge like a soap bubble
                    float a = 0.12f + 0.55f * d * d * d;
                    float line = 0;
                    foreach (Vector2 n in normals)
                    {
                        float t = Vector2.Dot(p, n) / spacing;
                        float f = Math.Abs(t - (float)Math.Round(t));
                        line = Math.Max(line, MathHelper.Clamp(1 - f / 0.07f, 0, 1));
                    }
                    a += line * 0.3f * (0.4f + d);
                    //bright rim
                    a = Math.Max(a, MathHelper.Clamp(1 - Math.Abs(d - 0.97f) / 0.04f, 0, 1));
                    //soft outer edge
                    a *= MathHelper.Clamp((1 - d) * r, 0, 1);
                    data[x + y * Size] = Color.White * MathHelper.Clamp(a, 0, 1);
                }
            }
            var tex = new Texture2D(gd, Size, Size);
            tex.SetData(data);
            return tex;
        }

        static Texture2D MakeCircle(GraphicsDevice gd, bool filled)
        {
            var data = new Color[Size * Size];
            float r = Size / 2f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r));
                    float a;
                    if (filled)
                        a = MathHelper.Clamp(r - d, 0, 1);
                    else
                        a = MathHelper.Clamp(1 - Math.Abs(d - (r - 3)) / 2.5f, 0, 1);
                    data[x + y * Size] = Color.White * a;
                }
            }
            var tex = new Texture2D(gd, Size, Size);
            tex.SetData(data);
            return tex;
        }

        public void Rect(SpriteBatch sb, Rectangle rect, Color color) => sb.Draw(Pixel, rect, color);

        public void RectOutline(SpriteBatch sb, Rectangle rect, Color color, int thickness = 1)
        {
            Rect(sb, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
            Rect(sb, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            Rect(sb, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
            Rect(sb, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
        }

        public void Circle(SpriteBatch sb, Vector2 center, float radius, Color color, bool filled = true)
        {
            Texture2D tex = filled ? Disc : Ring;
            sb.Draw(tex, center, null, color, 0, new Vector2(Size / 2f), radius * 2 / Size, SpriteEffects.None, 0);
        }
    }
}
