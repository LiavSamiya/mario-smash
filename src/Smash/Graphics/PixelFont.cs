using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //a tiny 5x7 bitmap font drawn from code, so the game needs no font files
    class PixelFont
    {
        const int GlyphWidth = 5;
        const int GlyphHeight = 7;
        const int Advance = GlyphWidth + 1;

        readonly Texture2D atlas;
        readonly Dictionary<char, int> index = new Dictionary<char, int>();

        public PixelFont(GraphicsDevice gd)
        {
            var chars = new List<char>(Glyphs.Keys);
            var data = new Color[chars.Count * Advance * GlyphHeight];
            int width = chars.Count * Advance;
            for (int i = 0; i < chars.Count; i++)
            {
                index[chars[i]] = i;
                string[] rows = Glyphs[chars[i]];
                for (int y = 0; y < GlyphHeight; y++)
                    for (int x = 0; x < GlyphWidth; x++)
                        if (rows[y][x] == '#')
                            data[i * Advance + x + y * width] = Color.White;
            }
            atlas = new Texture2D(gd, width, GlyphHeight);
            atlas.SetData(data);
        }

        public static Vector2 Measure(string text, float scale) =>
            new Vector2(text.Length == 0 ? 0 : (text.Length * Advance - 1) * scale, GlyphHeight * scale);

        public void Draw(SpriteBatch sb, string text, Vector2 position, Color color, float scale, bool shadow = true)
        {
            if (shadow)
                DrawRaw(sb, text, position + new Vector2(scale * 0.6f + 1), Color.Black * (color.A / 255f * 0.8f), scale);
            DrawRaw(sb, text, position, color, scale);
        }

        //draws text centred on a point
        public void DrawCentered(SpriteBatch sb, string text, Vector2 center, Color color, float scale, bool shadow = true)
        {
            Vector2 size = Measure(text, scale);
            Draw(sb, text, new Vector2((int)(center.X - size.X / 2), (int)(center.Y - size.Y / 2)), color, scale, shadow);
        }

        void DrawRaw(SpriteBatch sb, string text, Vector2 position, Color color, float scale)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);
                if (c == ' ' || !index.TryGetValue(c, out int g))
                    continue;
                var src = new Rectangle(g * Advance, 0, GlyphWidth, GlyphHeight);
                sb.Draw(atlas, position + new Vector2(i * Advance * scale, 0), src, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#." },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." },
            [','] = new[] { ".....", ".....", ".....", ".....", ".##..", "..#..", ".#..." },
            ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
            ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
            ['%'] = new[] { "##...", "##..#", "...#.", "..#..", ".#...", "#..##", "...##" },
            [':'] = new[] { ".....", ".##..", ".##..", ".....", ".##..", ".##..", "....." },
            ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['/'] = new[] { ".....", "....#", "...#.", "..#..", ".#...", "#....", "....." },
            ['('] = new[] { "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#." },
            [')'] = new[] { ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..." },
            ['\''] = new[] { "..#..", "..#..", ".#...", ".....", ".....", ".....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            ['<'] = new[] { "...#.", "..#..", ".#...", "#....", ".#...", "..#..", "...#." },
            ['>'] = new[] { ".#...", "..#..", "...#.", "....#", "...#.", "..#..", ".#..." },
            ['='] = new[] { ".....", ".....", "#####", ".....", "#####", ".....", "....." },
            ['^'] = new[] { "..#..", ".###.", "#.#.#", "..#..", "..#..", "..#..", "..#.." },
        };
    }
}
