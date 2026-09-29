using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //an axis aligned box with float edges, in world space
    struct Box
    {
        public float Left, Top, Right, Bottom;

        public Box(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Width => Right - Left;
        public float Height => Bottom - Top;
        public Vector2 Center => new Vector2((Left + Right) / 2, (Top + Bottom) / 2);

        public bool Contains(Vector2 p) => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;
        public bool Overlaps(Box o) => Left < o.Right && Right > o.Left && Top < o.Bottom && Bottom > o.Top;
        public Rectangle ToRectangle() => new Rectangle((int)Left, (int)Top, (int)Width, (int)Height);
    }

    //finds the solid platform (the "deck") of a stage image from its pixels
    static class StageSurface
    {
        //how much the top edge may vary and still count as the same flat surface
        const int FlatTolerance = 12;

        //returns the deck in pixel coordinates relative to the frame rectangle
        public static Rectangle FindDeck(SpriteSheet sheet, int frame)
        {
            Rectangle rect = sheet.Frames[frame];
            var tops = new int[rect.Width];
            for (int x = 0; x < rect.Width; x++)
            {
                tops[x] = -1;
                for (int y = 0; y < rect.Height; y++)
                {
                    if (sheet.IsSolid(rect.X + x, y))
                    {
                        tops[x] = y;
                        break;
                    }
                }
            }
            int highest = tops.Where(t => t >= 0).DefaultIfEmpty(-1).Min();
            if (highest < 0)
                throw new InvalidDataException($"Stage '{sheet.Name}' has no solid pixels");

            //the longest run of columns whose top is close to the highest point is the walkable deck
            int bestStart = 0, bestLength = 0;
            for (int x = 0; x < rect.Width;)
            {
                if (tops[x] < 0 || tops[x] > highest + FlatTolerance)
                {
                    x++;
                    continue;
                }
                int start = x;
                while (x < rect.Width && tops[x] >= 0 && tops[x] <= highest + FlatTolerance)
                    x++;
                if (x - start > bestLength)
                {
                    bestStart = start;
                    bestLength = x - start;
                }
            }

            int top = Median(Enumerable.Range(bestStart, bestLength).Select(x => tops[x]));

            //the thickness is measured near the edges of the deck, where nothing hangs below it
            int edge = Math.Max(2, bestLength / 16);
            var thickness = new List<int>();
            foreach (int x in Enumerable.Range(bestStart, edge).Concat(Enumerable.Range(bestStart + bestLength - edge, edge)))
            {
                int y = tops[x];
                while (y < rect.Height && sheet.IsSolid(rect.X + x, y))
                    y++;
                thickness.Add(y - tops[x]);
            }
            return new Rectangle(bestStart, top, bestLength, Math.Max(4, Median(thickness)));
        }

        static int Median(IEnumerable<int> values)
        {
            int[] sorted = values.OrderBy(v => v).ToArray();
            return sorted[sorted.Length / 2];
        }
    }

    //the animated floating platform and the space background behind it
    class Stage
    {
        public StageDefinition Def { get; }
        public SpriteSheet Sheet { get; }
        //the size of the background (the visible world)
        public Box Bounds { get; }
        //fighters that leave this box are knocked out
        public Box BlastZone { get; }
        //the solid platform, from one tip of the sprite to the other
        public Box Deck { get; }
        //the corners fighters can hang from: the very tips of the platform
        public Vector2 LedgeLeft { get; }
        public Vector2 LedgeRight { get; }
        //height of the walkable surface for every sprite column from the left tip
        readonly float[] surface;
        //world position of the stage image's handle
        public Vector2 Position { get; }

        readonly string backgroundPath;
        Texture2D texture, background;
        int frame, ticks;

        public Stage(string folder)
        {
            Def = DataLoader.Load<StageDefinition>(Path.Combine(folder, "stage.json"));
            Sheet = SpriteSheet.Load(Path.Combine(folder, Def.Sheet + ".png"));
            backgroundPath = Path.Combine(folder, Def.Background + ".png");
            ImageData bg = ImageData.Load(backgroundPath);
            Bounds = new Box(0, 0, bg.Width * Def.BackgroundScale, bg.Height * Def.BackgroundScale);
            BlastZone = new Box(Bounds.Left - Def.BlastMargin, Bounds.Top - Def.BlastMargin, Bounds.Right + Def.BlastMargin, Bounds.Bottom + Def.BlastMargin);

            //place the image so its deck is centred on CenterX with its surface at DeckTopY
            Rectangle deck = StageSurface.FindDeck(Sheet, 0);
            deck = new Rectangle(deck.X, deck.Y + Def.SurfaceDepth, deck.Width, deck.Height - Def.SurfaceDepth);
            Vector2 origin = Sheet.Origins[0];
            float s = Def.Scale;
            Position = new Vector2(
                Def.CenterX - (deck.Center.X - origin.X) * s,
                Def.DeckTopY - (deck.Top - origin.Y) * s);
            //the walkable surface follows the top edge of the platform out to its sloped tips
            Rectangle rect = Sheet.Frames[0];
            int flatTop = deck.Top - Def.SurfaceDepth;
            bool SolidAt(int col) => col >= 0 && col < rect.Width && Enumerable.Range(flatTop, deck.Bottom - flatTop).Any(y => Sheet.IsSolid(rect.X + col, y));
            int tipLeft = deck.Left, tipRight = deck.Right - 1;
            while (SolidAt(tipLeft - 1)) tipLeft--;
            while (SolidAt(tipRight + 1)) tipRight++;
            var rows = new float[tipRight - tipLeft + 1];
            for (int c = tipLeft; c <= tipRight; c++)
            {
                int top = Enumerable.Range(0, rect.Height).FirstOrDefault(y => Sheet.IsSolid(rect.X + c, y));
                //decorations sticking up from the middle of the deck do not raise the floor
                rows[c - tipLeft] = Math.Max(top, flatTop) + Def.SurfaceDepth;
            }
            surface = new float[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                //smooth out single-pixel steps
                int from = Math.Max(0, i - 3), to = Math.Min(rows.Length - 1, i + 3);
                float avg = 0;
                for (int k = from; k <= to; k++) avg += rows[k];
                surface[i] = Position.Y + (avg / (to - from + 1) - origin.Y) * s;
            }
            Deck = new Box(
                Position.X + (tipLeft - origin.X) * s,
                surface.Min(),
                Position.X + (tipRight + 1 - origin.X) * s,
                Position.Y + (deck.Bottom - origin.Y) * s);
            LedgeLeft = new Vector2(Deck.Left, SurfaceY(Deck.Left));
            LedgeRight = new Vector2(Deck.Right, SurfaceY(Deck.Right));
        }

        //world height of the walkable surface at a world x inside the deck
        public float SurfaceY(float x)
        {
            float col = MathHelper.Clamp((x - Deck.Left) / Def.Scale, 0, surface.Length - 1);
            int i = (int)col;
            int j = Math.Min(i + 1, surface.Length - 1);
            return MathHelper.Lerp(surface[i], surface[j], col - i);
        }

        public static Stage LoadDefault() => new Stage(Path.Combine(G.ContentRoot, "Stages"));

        //starting positions spread over the deck
        public Vector2 SpawnPoint(int index, int count)
        {
            float step = Deck.Width / (count + 1);
            float x = Deck.Left + step * (index + 1);
            return new Vector2(x, SurfaceY(x));
        }

        //respawn positions above the stage
        public Vector2 RespawnPoint(int index) =>
            new Vector2(Deck.Center.X + (index % 2 == 0 ? -1 : 1) * (60 + 60 * (index / 2)), Deck.Top - 260);

        public void Update()
        {
            if (++ticks < Def.FrameTicks)
                return;
            ticks = 0;
            frame = (frame + 1) % Sheet.FrameCount;
        }

        public void LoadTextures(GraphicsDevice gd)
        {
            texture ??= Sheet.Image.ToTexture(gd);
            background ??= ImageData.Load(backgroundPath).ToTexture(gd);
        }

        public void DrawBackground(SpriteBatch sb)
        {
            sb.Draw(background, new Vector2(Bounds.Left, Bounds.Top), null, Color.White, 0, Vector2.Zero, Def.BackgroundScale, SpriteEffects.None, 0);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(texture, Position, Sheet.Frames[frame], Color.White, 0, Sheet.Origins[frame], Def.Scale, SpriteEffects.None, 0);
        }
    }
}
