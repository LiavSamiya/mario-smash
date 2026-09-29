using System;
using Microsoft.Xna.Framework;

namespace Smash
{
    //alternative colours ("skins") for any character: the colourful parts of the sprite get a new hue,
    //while skin tones, greys, black and white stay as they are
    static class Palette
    {
        public static readonly string[] Names = { "CLASSIC", "EMERALD", "OCEAN", "ROYAL", "SUNSET", "SHADOW" };

        //hue rotation in degrees; the last palette is a dark, desaturated look
        static readonly float[] hueShift = { 0, 120, 200, 270, 40, 0 };

        public static int Count => Names.Length;

        public static ImageData Apply(ImageData source, int palette)
        {
            if (palette == 0)
                return source;

            ImageData result = source.Clone();
            bool shadow = palette == Count - 1;
            for (int i = 0; i < result.Pixels.Length; i++)
            {
                Color c = result.Pixels[i];
                if (c.A == 0)
                    continue;
                RgbToHsv(c, out float h, out float s, out float v);
                if (shadow)
                {
                    result.Pixels[i] = HsvToRgb(h, s * 0.35f, v * 0.55f, c.A);
                    continue;
                }
                if (s < 0.3f || v < 0.15f || IsSkin(h, s, v))
                    continue;
                result.Pixels[i] = HsvToRgb((h + hueShift[palette]) % 360, s, v, c.A);
            }
            return result;
        }

        //orange-ish, not too saturated and fairly bright
        static bool IsSkin(float h, float s, float v) => h >= 12 && h <= 45 && s < 0.65f && v > 0.45f;

        static void RgbToHsv(Color c, out float h, out float s, out float v)
        {
            float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
            float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            float d = max - min;
            v = max;
            s = max == 0 ? 0 : d / max;
            if (d == 0) h = 0;
            else if (max == r) h = 60 * (((g - b) / d + 6) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }

        static Color HsvToRgb(float h, float s, float v, byte a)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            float m = v - c;
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return new Color((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255), a);
        }
    }
}
