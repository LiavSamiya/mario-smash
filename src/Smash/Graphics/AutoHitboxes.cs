using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Smash
{
    //where an automatically generated hitbox is placed on a frame
    enum HitboxDirection
    {
        //the front-most point of the sprite (sprites face right)
        Forward,
        //the back-most point of the sprite
        Back,
        //the highest point of the sprite
        Up,
        //the point furthest up and forward (a headbutt or an uppercut arc)
        UpForward,
        //the lowest point of the sprite
        Down,
        //both the front-most and the back-most points
        Sides,
        //one circle around the whole body
        Around,
    }

    //builds circles from a frame's silhouette, for animations that have no hand made mask
    static class AutoHitboxes
    {
        //covers the silhouette with three circles along its longest side
        public static List<Circle> Hurtboxes(SpriteSheet sheet, int frame)
        {
            var circles = new List<Circle>();
            Rectangle box = Bounds(sheet, frame);
            if (box.Width == 0)
                return circles;

            const int segments = 3;
            bool vertical = box.Height >= box.Width;
            int length = vertical ? box.Height : box.Width;
            for (int s = 0; s < segments; s++)
            {
                int from = s * length / segments;
                int to = (s + 1) * length / segments;
                Vector2 sum = Vector2.Zero;
                int count = 0, minA = int.MaxValue, maxA = int.MinValue;
                for (int y = box.Top; y < box.Bottom; y++)
                {
                    for (int x = box.Left; x < box.Right; x++)
                    {
                        int along = vertical ? y - box.Top : x - box.Left;
                        if (along < from || along >= to || !sheet.IsSolid(x, y))
                            continue;
                        sum += new Vector2(x, y);
                        count++;
                        int across = vertical ? x : y;
                        minA = Math.Min(minA, across);
                        maxA = Math.Max(maxA, across);
                    }
                }
                if (count == 0)
                    continue;
                float radius = Math.Max(to - from, maxA - minA + 1) / 2f * 0.85f;
                circles.Add(new Circle(sum / count - FrameOrigin(sheet, frame), radius));
            }
            return circles;
        }

        //places attack circles on the extreme point of the silhouette in the given direction
        public static List<Circle> Hitboxes(SpriteSheet sheet, int frame, HitboxDirection direction, float radius)
        {
            var circles = new List<Circle>();
            Rectangle box = Bounds(sheet, frame);
            if (box.Width == 0)
                return circles;

            Vector2 origin = FrameOrigin(sheet, frame);
            float inset = radius * 0.6f;
            switch (direction)
            {
                case HitboxDirection.Forward:
                    circles.Add(new Circle(new Vector2(box.Right - 1 - inset, AverageY(sheet, box, box.Right - 3, box.Right)) - origin, radius));
                    break;
                case HitboxDirection.Back:
                    circles.Add(new Circle(new Vector2(box.Left + inset, AverageY(sheet, box, box.Left, box.Left + 3)) - origin, radius));
                    break;
                case HitboxDirection.Sides:
                    circles.Add(new Circle(new Vector2(box.Right - 1 - inset, AverageY(sheet, box, box.Right - 3, box.Right)) - origin, radius));
                    circles.Add(new Circle(new Vector2(box.Left + inset, AverageY(sheet, box, box.Left, box.Left + 3)) - origin, radius));
                    break;
                case HitboxDirection.Up:
                    circles.Add(new Circle(new Vector2(AverageX(sheet, box, box.Top, box.Top + 3), box.Top + inset) - origin, radius));
                    break;
                case HitboxDirection.UpForward:
                    circles.Add(new Circle(ExtremeUpForward(sheet, box) + new Vector2(-inset, inset) * 0.7f - origin, radius));
                    break;
                case HitboxDirection.Down:
                    circles.Add(new Circle(new Vector2(AverageX(sheet, box, box.Bottom - 3, box.Bottom), box.Bottom - 1 - inset) - origin, radius));
                    break;
                case HitboxDirection.Around:
                    circles.Add(new Circle(box.Center.ToVector2() - origin, Math.Max(radius, Math.Max(box.Width, box.Height) / 2f * 0.8f)));
                    break;
            }
            return circles;
        }

        //bounding box of the solid pixels of a frame, in image coordinates
        public static Rectangle Bounds(SpriteSheet sheet, int frame)
        {
            Rectangle rect = sheet.Frames[frame];
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    if (!sheet.IsSolid(x, y))
                        continue;
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
            if (minX == int.MaxValue)
                return Rectangle.Empty;
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        //the solid pixel with the largest (x - y): furthest forward and up at the same time
        static Vector2 ExtremeUpForward(SpriteSheet sheet, Rectangle box)
        {
            Vector2 best = box.Center.ToVector2();
            float bestScore = float.MinValue;
            for (int y = box.Top; y < box.Bottom; y++)
            {
                for (int x = box.Left; x < box.Right; x++)
                {
                    if (sheet.IsSolid(x, y) && x - y > bestScore)
                    {
                        bestScore = x - y;
                        best = new Vector2(x, y);
                    }
                }
            }
            return best;
        }

        static Vector2 FrameOrigin(SpriteSheet sheet, int frame) => new Vector2(sheet.Frames[frame].X, 0) + sheet.Origins[frame];

        static float AverageY(SpriteSheet sheet, Rectangle box, int fromX, int toX)
        {
            float sum = 0;
            int count = 0;
            for (int x = Math.Max(fromX, box.Left); x < Math.Min(toX, box.Right); x++)
                for (int y = box.Top; y < box.Bottom; y++)
                    if (sheet.IsSolid(x, y)) { sum += y; count++; }
            return count > 0 ? sum / count : box.Center.Y;
        }

        static float AverageX(SpriteSheet sheet, Rectangle box, int fromY, int toY)
        {
            float sum = 0;
            int count = 0;
            for (int y = Math.Max(fromY, box.Top); y < Math.Min(toY, box.Bottom); y++)
                for (int x = box.Left; x < box.Right; x++)
                    if (sheet.IsSolid(x, y)) { sum += x; count++; }
            return count > 0 ? sum / count : box.Center.X;
        }
    }
}
