using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbImageSharp;

namespace Smash
{
    //raw pixels of an image. kept separate from Texture2D so sprite sheets and masks
    //can be analysed without a graphics device (for example in unit tests)
    class ImageData
    {
        public int Width { get; }
        public int Height { get; }
        public Color[] Pixels { get; }

        public ImageData(int width, int height, Color[] pixels)
        {
            if (pixels.Length != width * height)
                throw new ArgumentException("Pixel count does not match the image size");
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public Color this[int x, int y]
        {
            get => Pixels[x + y * Width];
            set => Pixels[x + y * Width] = value;
        }

        public static ImageData Load(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                ImageResult img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                var pixels = new Color[img.Width * img.Height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int p = i * 4;
                    pixels[i] = new Color(img.Data[p], img.Data[p + 1], img.Data[p + 2], img.Data[p + 3]);
                }
                return new ImageData(img.Width, img.Height, pixels);
            }
        }

        public ImageData Clone() => new ImageData(Width, Height, (Color[])Pixels.Clone());

        //uploads the image to the GPU (SpriteBatch expects premultiplied alpha)
        public Texture2D ToTexture(GraphicsDevice gd)
        {
            var data = new Color[Pixels.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Color c = Pixels[i];
                data[i] = c.A == 255 ? c : Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
            }
            var tex = new Texture2D(gd, Width, Height);
            tex.SetData(data);
            return tex;
        }
    }
}
