using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace Smash
{
    //cuts the frames of one animation out of a full sprite sheet and lays them side by side in a strip
    static class SheetBuilder
    {
        //a pixel this close to the background colour (on every channel) is background.
        //must match Tolerance in tools/SpriteCutter
        const int Tolerance = 48;
        //after the background is removed, edge pixels still tinted by the background (JPEG halo) are removed too
        const int FringeTolerance = 80;

        //full sheets are big, so every file is loaded only once
        static readonly Dictionary<string, ImageData> cache = new Dictionary<string, ImageData>();

        public static ImageData LoadSheet(string path)
        {
            lock (cache)
            {
                if (!cache.TryGetValue(path, out ImageData img))
                {
                    img = ImageData.Load(path);
                    cache[path] = img;
                }
                return img;
            }
        }

        public static SpriteSheet Build(string name, ImageData sheet, Color background, bool flip, int[][] frames)
        {
            if (frames == null || frames.Length == 0)
                throw new InvalidDataException($"Animation '{name}' has no frames");

            int width = 0, height = 0;
            foreach (int[] f in frames)
            {
                if (f.Length != 6)
                    throw new InvalidDataException($"A frame of '{name}' must be [x, y, width, height, handleX, handleY]");
                if (f[0] < 0 || f[1] < 0 || f[0] + f[2] > sheet.Width || f[1] + f[3] > sheet.Height)
                    throw new InvalidDataException($"A frame of '{name}' is outside the sprite sheet");
                width += f[2] + 1;
                height = Math.Max(height, f[3]);
            }

            var strip = new ImageData(width, height, new Color[width * height]);
            var rects = new List<Rectangle>();
            var origins = new List<Vector2>();
            int x = 0;
            foreach (int[] f in frames)
            {
                ImageData crop = Crop(sheet, f[0], f[1], f[2], f[3], background);
                for (int y = 0; y < crop.Height; y++)
                    for (int cx = 0; cx < crop.Width; cx++)
                        strip[x + (flip ? crop.Width - 1 - cx : cx), y] = crop[cx, y];
                rects.Add(new Rectangle(x, 0, f[2], f[3]));
                origins.Add(new Vector2(flip ? f[2] - f[4] : f[4], f[5]));
                x += f[2] + 1;
            }
            return new SpriteSheet(name, strip, rects, origins);
        }

        //copies a rectangle and makes the background transparent: everything background coloured that
        //can be reached from the border is removed, so background-like colours inside the sprite survive
        static ImageData Crop(ImageData sheet, int x0, int y0, int w, int h, Color bg)
        {
            var img = new ImageData(w, h, new Color[w * h]);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    img[x, y] = sheet[x0 + x, y0 + y];

            var clear = new bool[w * h];
            var stack = new Stack<int>();
            for (int x = 0; x < w; x++) { stack.Push(x); stack.Push(x + (h - 1) * w); }
            for (int y = 0; y < h; y++) { stack.Push(y * w); stack.Push(w - 1 + y * w); }
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (clear[i] || Distance(img.Pixels[i], bg) > Tolerance)
                    continue;
                clear[i] = true;
                int px = i % w, py = i / w;
                if (px > 0) stack.Push(i - 1);
                if (px < w - 1) stack.Push(i + 1);
                if (py > 0) stack.Push(i - w);
                if (py < h - 1) stack.Push(i + w);
            }

            //one pass of halo removal on the outline
            var fringe = new List<int>();
            for (int i = 0; i < clear.Length; i++)
            {
                if (clear[i] || Distance(img.Pixels[i], bg) > FringeTolerance)
                    continue;
                int px = i % w, py = i / w;
                bool edge = (px > 0 && clear[i - 1]) || (px < w - 1 && clear[i + 1]) || (py > 0 && clear[i - w]) || (py < h - 1 && clear[i + w]);
                if (edge)
                    fringe.Add(i);
            }
            foreach (int i in fringe)
                clear[i] = true;

            for (int i = 0; i < clear.Length; i++)
                if (clear[i])
                    img.Pixels[i] = Color.Transparent;
            return img;
        }

        static int Distance(Color a, Color b) =>
            Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
    }
}
