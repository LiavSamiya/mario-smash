using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Smash
{
    //a horizontal strip of animation frames.
    //the last row of pixels holds black markers: start of frame, origin of frame, start of next frame, ...
    //the origin marker is the "handle" of the frame (the point between the feet)
    class SpriteSheet
    {
        public string Name { get; }
        //the image with its background made transparent
        public ImageData Image { get; }
        //the original colour of the background
        public Color Background { get; }
        //frame rectangles inside the image
        public List<Rectangle> Frames { get; } = new List<Rectangle>();
        //handles of each frame, relative to the frame rectangle
        public List<Vector2> Origins { get; } = new List<Vector2>();

        public int FrameCount => Frames.Count;

        public SpriteSheet(string name, ImageData raw)
        {
            Name = name;
            List<int> markers = FindMarkers(raw);
            if (markers.Count < 2)
                throw new InvalidDataException($"Sprite sheet '{name}' has no frame markers in its last row");

            int frameHeight = raw.Height - 1;
            for (int i = 0; i + 1 < markers.Count; i += 2)
            {
                int start = markers[i];
                int end = i + 2 < markers.Count ? markers[i + 2] : raw.Width;
                Frames.Add(new Rectangle(start, 0, end - start, frameHeight));
                Origins.Add(new Vector2(markers[i + 1] - start, frameHeight));
            }

            Background = MostCommonColor(raw);
            Image = raw.Clone();
            MakeBackgroundTransparent(Image, Background);
        }

        //a strip that is already cut into frames (see SheetBuilder)
        public SpriteSheet(string name, ImageData image, List<Rectangle> frames, List<Vector2> origins)
        {
            Name = name;
            Image = image;
            Background = Color.Transparent;
            Frames.AddRange(frames);
            Origins.AddRange(origins);
        }

        public static SpriteSheet Load(string path) => new SpriteSheet(Path.GetFileNameWithoutExtension(path), ImageData.Load(path));

        //x positions of the pure black pixels in the last row
        public static List<int> FindMarkers(ImageData img)
        {
            var markers = new List<int>();
            int y = img.Height - 1;
            for (int x = 0; x < img.Width; x++)
            {
                Color c = img[x, y];
                if (c.R == 0 && c.G == 0 && c.B == 0 && c.A == 255)
                    markers.Add(x);
            }
            return markers;
        }

        //the background is the most common colour of the image (the marker row is ignored)
        public static Color MostCommonColor(ImageData img)
        {
            var counts = new Dictionary<uint, int>();
            int n = img.Width * (img.Height - 1);
            for (int i = 0; i < n; i++)
            {
                uint key = img.Pixels[i].PackedValue;
                counts.TryGetValue(key, out int c);
                counts[key] = c + 1;
            }
            uint best = counts.OrderByDescending(kv => kv.Value).First().Key;
            return new Color { PackedValue = best };
        }

        static void MakeBackgroundTransparent(ImageData img, Color background)
        {
            for (int i = 0; i < img.Pixels.Length; i++)
            {
                if (img.Pixels[i] == background || img.Pixels[i].A == 0)
                    img.Pixels[i] = Color.Transparent;
            }
            //the marker row is never drawn
            for (int x = 0; x < img.Width; x++)
                img[x, img.Height - 1] = Color.Transparent;
        }

        //true when the pixel belongs to the character, not the background
        public bool IsSolid(int x, int y) => Image[x, y].A > 0;
    }
}
