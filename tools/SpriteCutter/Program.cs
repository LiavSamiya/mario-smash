using System.Text.Json;
using System.Text.Json.Nodes;
using StbImageSharp;
using StbImageWriteSharp;

//SpriteCutter: finds the separate sprites on a full sprite sheet and turns a list of picked sprites into
//the frames.json file the game loads.
//
//  SpriteCutter detect <sheet image> <output folder>
//      numbers every sprite on the sheet and writes annotated images (band*.png) to pick frames from
//
//  SpriteCutter build <cut file> <sheets folder> <character folder> [preview folder]
//      reads the picked sprite numbers from the cut file, lines the frames up and writes frames.json
static class Program
{
    static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "detect")
        {
            int[] range = args.Length > 3 ? args[3].Split(':').Select(int.Parse).ToArray() : new[] { 0, int.MaxValue };
            Detect(args[1], args[2], range[0], range[1]);
            return 0;
        }
        if (args.Length >= 4 && args[0] == "build")
        {
            Build(args[1], args[2], args[3], args.Length > 4 ? args[4] : null);
            return 0;
        }
        Console.WriteLine("usage: SpriteCutter detect <sheet> <out folder>");
        Console.WriteLine("       SpriteCutter build <cut file> <sheets folder> <character folder> [preview folder]");
        return 1;
    }

    #region image
    class Img
    {
        public int W, H;
        public byte[] Rgba;
        public (int r, int g, int b, int a) Get(int x, int y)
        {
            int i = (x + y * W) * 4;
            return (Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]);
        }
        public void Set(int x, int y, int r, int g, int b, int a = 255)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = (x + y * W) * 4;
            Rgba[i] = (byte)r; Rgba[i + 1] = (byte)g; Rgba[i + 2] = (byte)b; Rgba[i + 3] = (byte)a;
        }
    }

    static Img Load(string path)
    {
        using var s = File.OpenRead(path);
        var r = ImageResult.FromStream(s, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        return new Img { W = r.Width, H = r.Height, Rgba = r.Data };
    }

    static void Save(Img img, string path)
    {
        using var s = File.Create(path);
        new ImageWriter().WritePng(img.Rgba, img.W, img.H, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, s);
    }

    //the most common colour is the background
    static (int r, int g, int b) Background(Img img)
    {
        var counts = new Dictionary<int, int>();
        for (int i = 0; i < img.W * img.H; i++)
        {
            int key = img.Rgba[i * 4] << 16 | img.Rgba[i * 4 + 1] << 8 | img.Rgba[i * 4 + 2];
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }
        int best = counts.MaxBy(kv => kv.Value).Key;
        return (best >> 16 & 255, best >> 8 & 255, best & 255);
    }
    #endregion

    #region detection
    //must match SheetBuilder.Tolerance in the game
    const int Tolerance = 48;
    const int MinPixels = 12;
    const int MergeGap = 3;
    const int SmallPiece = 40;

    record Box(int X, int Y, int W, int H, int Row)
    {
        public int Right => X + W;
        public int Bottom => Y + H;
    }

    static bool[] Foreground(Img img, (int r, int g, int b) bg)
    {
        var fg = new bool[img.W * img.H];
        for (int y = 0; y < img.H; y++)
            for (int x = 0; x < img.W; x++)
            {
                var p = img.Get(x, y);
                fg[x + y * img.W] = p.a > 0 && Math.Max(Math.Abs(p.r - bg.r), Math.Max(Math.Abs(p.g - bg.g), Math.Abs(p.b - bg.b))) > Tolerance;
            }
        return fg;
    }

    static List<Box> Components(Img img)
    {
        bool[] fg = Foreground(img, Background(img));
        var seen = new bool[fg.Length];
        var rects = new List<(int x0, int y0, int x1, int y1)>();
        var sizes = new List<int>();
        var stack = new Stack<int>();
        for (int start = 0; start < fg.Length; start++)
        {
            if (!fg[start] || seen[start]) continue;
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1, count = 0;
            stack.Push(start);
            seen[start] = true;
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % img.W, y = i / img.W;
                count++;
                x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= img.W || ny >= img.H) continue;
                        int n = nx + ny * img.W;
                        if (fg[n] && !seen[n]) { seen[n] = true; stack.Push(n); }
                    }
            }
            if (count >= MinPixels)
            {
                rects.Add((x0, y0, x1 + 1, y1 + 1));
                sizes.Add(count);
            }
        }

        //tiny pieces (a detached hand, a spark) join the sprite right next to them.
        //bigger pieces are joined by hand in the cut file, so neighbouring frames never merge
        for (int i = rects.Count - 1; i >= 0; i--)
        {
            if (sizes[i] >= SmallPiece) continue;
            var a = rects[i];
            int target = -1;
            for (int j = 0; j < rects.Count; j++)
            {
                if (j == i || sizes[j] < SmallPiece) continue;
                var b = rects[j];
                int gapX = Math.Max(a.x0, b.x0) - Math.Min(a.x1, b.x1);
                int gapY = Math.Max(a.y0, b.y0) - Math.Min(a.y1, b.y1);
                if (gapX <= MergeGap && gapY <= MergeGap) { target = j; break; }
            }
            if (target < 0) continue;
            var t = rects[target];
            rects[target] = (Math.Min(a.x0, t.x0), Math.Min(a.y0, t.y0), Math.Max(a.x1, t.x1), Math.Max(a.y1, t.y1));
            rects.RemoveAt(i);
            sizes.RemoveAt(i);
        }

        //number the boxes in reading order: rows from top to bottom, left to right inside a row
        rects = SplitWide(rects, fg, img.W);

        //a box joins a row when at least half of its height overlaps the row
        var rows = new List<List<(int x0, int y0, int x1, int y1)>>();
        var rowRanges = new List<(int top, int bottom)>();
        foreach (var r in rects.OrderBy(r => r.y1 - r.y0).ThenBy(r => r.y0))
        {
            int h = r.y1 - r.y0, found = -1;
            for (int k = 0; k < rows.Count && found < 0; k++)
            {
                int overlap = Math.Min(r.y1, rowRanges[k].bottom) - Math.Max(r.y0, rowRanges[k].top);
                int rowH = rowRanges[k].bottom - rowRanges[k].top;
                if (overlap * 2 >= h && overlap * 2 >= Math.Min(h, rowH))
                    found = k;
            }
            if (found < 0)
            {
                rows.Add(new());
                rowRanges.Add((r.y0, r.y1));
                found = rows.Count - 1;
            }
            rows[found].Add(r);
            rowRanges[found] = (Math.Min(rowRanges[found].top, r.y0), Math.Max(rowRanges[found].bottom, r.y1));
        }
        var order = Enumerable.Range(0, rows.Count).OrderBy(k => rowRanges[k].top).ToList();
        rows = order.Select(k => rows[k]).ToList();
        var boxes = new List<Box>();
        for (int row = 0; row < rows.Count; row++)
            foreach (var r in rows[row].OrderBy(r => r.x0))
                boxes.Add(new Box(r.x0, r.y0, r.x1 - r.x0, r.y1 - r.y0, row));
        return boxes;
    }

    //frames drawn touching each other come out as one wide box: cut it at the emptiest columns
    static List<(int x0, int y0, int x1, int y1)> SplitWide(List<(int x0, int y0, int x1, int y1)> rects, bool[] fg, int width)
    {
        var tall = rects.Where(r => r.y1 - r.y0 >= 14).ToList();
        if (tall.Count == 0) return rects;
        int typical = tall.Select(r => r.x1 - r.x0).OrderBy(w => w).ElementAt(tall.Count / 2);
        var result = new List<(int x0, int y0, int x1, int y1)>();
        foreach (var r in rects)
        {
            if (r.x1 - r.x0 < typical * 1.7 || r.y1 - r.y0 < 14 || r.y1 - r.y0 > typical * 4)
            {
                result.Add(r);
                continue;
            }
            var columns = new int[r.x1 - r.x0];
            for (int x = r.x0; x < r.x1; x++)
                for (int y = r.y0; y < r.y1; y++)
                    if (fg[x + y * width]) columns[x - r.x0]++;
            int max = columns.Max();
            if (Environment.GetEnvironmentVariable("SPLITDEBUG") != null) Console.WriteLine($"typical {typical} box {r} cols " + string.Join(",", columns));
            int start = 0;
            var cuts = new List<int>();
            while (columns.Length - start > typical * 1.7)
            {
                int best = -1;
                for (int c = start + (int)(typical * 0.6); c < Math.Min(columns.Length - (int)(typical * 0.6), start + (int)(typical * 1.6)); c++)
                    if (best < 0 || columns[c] < columns[best] || (columns[c] == columns[best] && Math.Abs(c - start - typical) < Math.Abs(best - start - typical)))
                        best = c;
                if (best < 0 || columns[best] > max * 0.3) break;
                cuts.Add(best);
                start = best + 1;
            }
            int from = 0;
            foreach (int cut in cuts.Append(columns.Length))
            {
                var piece = Trim(r.x0 + from, r.y0, r.x0 + cut, r.y1, fg, width);
                if (piece.x1 > piece.x0) result.Add(piece);
                from = cut + 1;
            }
        }
        return result;
    }

    static (int x0, int y0, int x1, int y1) Trim(int x0, int y0, int x1, int y1, bool[] fg, int width)
    {
        int a = int.MaxValue, b = int.MaxValue, c = -1, d = -1;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                if (fg[x + y * width]) { a = Math.Min(a, x); b = Math.Min(b, y); c = Math.Max(c, x); d = Math.Max(d, y); }
        return c < 0 ? (0, 0, 0, 0) : (a, b, c + 1, d + 1);
    }

    static void Detect(string sheet, string outDir, int minX, int maxX)
    {
        Directory.CreateDirectory(outDir);
        Img img = Load(sheet);
        List<Box> boxes = Components(img);
        Console.WriteLine($"{boxes.Count} sprites, background {Background(img)}");
        bool[] fgDebug = Foreground(img, Background(img));
        var maskImg = new Img { W = img.W, H = img.H, Rgba = new byte[img.W * img.H * 4] };
        for (int i = 0; i < fgDebug.Length; i++) { byte v = (byte)(fgDebug[i] ? 255 : 0); maskImg.Rgba[i * 4] = v; maskImg.Rgba[i * 4 + 1] = v; maskImg.Rgba[i * 4 + 2] = v; maskImg.Rgba[i * 4 + 3] = 255; }
        Save(maskImg, Path.Combine(outDir, "foreground.png"));

        //annotated copy at 3x, cut into horizontal bands that are easy to look at
        const int scale = 3;
        const int band = 180;
        int width = Math.Min(img.W, maxX) - minX;
        for (int top = 0, n = 0; top < img.H; top += band, n++)
        {
            int h = Math.Min(band, img.H - top);
            var o = new Img { W = width * scale, H = h * scale, Rgba = new byte[width * scale * h * scale * 4] };
            for (int y = 0; y < o.H; y++)
                for (int x = 0; x < o.W; x++)
                {
                    var p = img.Get(minX + x / scale, top + y / scale);
                    o.Set(x, y, p.r, p.g, p.b);
                }
            for (int i = 0; i < boxes.Count; i++)
            {
                Box b = boxes[i];
                if (b.Bottom < top || b.Y > top + h) continue;
                var c = i % 2 == 0 ? (255, 0, 255) : (255, 255, 0);
                int x0 = (b.X - minX) * scale, y0 = (b.Y - top) * scale, x1 = (b.Right - minX) * scale, y1 = (b.Bottom - top) * scale;
                for (int x = x0; x <= x1; x++) { o.Set(x, y0, c.Item1, c.Item2, c.Item3); o.Set(x, y1, c.Item1, c.Item2, c.Item3); }
                for (int y = y0; y <= y1; y++) { o.Set(x0, y, c.Item1, c.Item2, c.Item3); o.Set(x1, y, c.Item1, c.Item2, c.Item3); }
                DrawNumber(o, i, x0 + 1, y0 + 1, 2);
            }
            Save(o, Path.Combine(outDir, $"band_x{minX}_y{top}.png"));
        }
        File.WriteAllText(Path.Combine(outDir, "sprites.txt"),
            string.Join("\n", boxes.Select((b, i) => $"{i}: x={b.X} y={b.Y} w={b.W} h={b.H} row={b.Row}")));
    }

    static readonly string[] Digits =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111",
    };

    static void DrawNumber(Img o, int value, int x, int y, int s)
    {
        string text = value.ToString();
        int w = text.Length * 4 * s + s, h = 7 * s;
        for (int yy = 0; yy < h; yy++)
            for (int xx = 0; xx < w; xx++)
                o.Set(x + xx, y + yy, 0, 0, 0);
        for (int c = 0; c < text.Length; c++)
        {
            string g = Digits[text[c] - '0'];
            for (int gy = 0; gy < 5; gy++)
                for (int gx = 0; gx < 3; gx++)
                    if (g[gy * 3 + gx] == '1')
                        for (int py = 0; py < s; py++)
                            for (int px = 0; px < s; px++)
                                o.Set(x + s + (c * 4 + gx) * s + px, y + s + gy * s + py, 255, 255, 255);
        }
    }
    #endregion

    #region build
    //one frame in sheet coordinates plus its handle (origin) relative to the frame rectangle
    record Frame(int X, int Y, int W, int H, int Ox, int Oy, int Row);

    static void Build(string cutFile, string sheetsDir, string characterDir, string previewDir)
    {
        JsonNode cut = JsonNode.Parse(File.ReadAllText(cutFile), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        string sheetName = (string)cut["sheet"];
        Img img = Load(Path.Combine(sheetsDir, sheetName));
        var bg = Background(img);
        bool[] fg = Foreground(img, bg);
        List<Box> boxes = Components(img);

        Box Resolve(JsonNode node)
        {
            if (node is JsonValue val)
                return boxes[(int)val];
            //{ "split": id, "part": k, "parts": n }: one of n poses inside a box that holds several touching frames
            if (node is JsonObject s && s["split"] != null)
            {
                Box whole = boxes[(int)s["split"]];
                int count = (int)s["parts"], part = (int)s["part"];
                int from = whole.X + whole.W * part / count, to = whole.X + whole.W * (part + 1) / count;
                var t = Trim(from, whole.Y, to, whole.Bottom, fg, img.W);
                return new Box(t.x0, t.y0, t.x1 - t.x0, t.y1 - t.y0, whole.Row);
            }
            if (node is JsonObject rect)
                return new Box((int)rect["x"], (int)rect["y"], (int)rect["w"], (int)rect["h"], (int?)rect["row"] ?? -1);
            //a list of sprite numbers joined into one frame (for effects detached from the body)
            var parts = ((JsonArray)node).Select(n => boxes[(int)n]).ToList();
            int x0 = parts.Min(p => p.X), y0 = parts.Min(p => p.Y), x1 = parts.Max(p => p.Right), y1 = parts.Max(p => p.Bottom);
            return new Box(x0, y0, x1 - x0, y1 - y0, parts[0].Row);
        }

        var anims = (JsonObject)cut["animations"];
        string referenceName = (string)cut["reference"] ?? "Stand";
        Box refBox = Resolve(anims[referenceName]["frames"][0]);
        bool[,] refMask = Mask(fg, img.W, refBox);
        int refOx = FeetCenter(refMask);

        var output = new JsonObject
        {
            ["generatedBy"] = "tools/SpriteCutter (do not edit by hand, edit the cut file and rebuild)",
            ["sheet"] = sheetName,
            ["flip"] = (bool?)cut["flip"] ?? false,
            ["background"] = new JsonArray(bg.r, bg.g, bg.b),
        };
        var outAnims = new JsonObject();
        foreach (var (name, node) in anims)
        {
            string align = (string)node["align"] ?? "reference";
            var frameBoxes = ((JsonArray)node["frames"]).Select(Resolve).ToList();
            //frames drawn on the same row share a baseline, so a jump drawn higher stays higher
            var baselines = frameBoxes.GroupBy(b => b.Row).ToDictionary(g => g.Key, g => g.Max(b => b.Bottom));
            var frames = new List<Frame>();
            foreach (Box b in frameBoxes)
            {
                bool[,] mask = Mask(fg, img.W, b);
                int baseline = b.Row >= 0 ? baselines[b.Row] : b.Bottom;
                int oy = baseline - b.Y;
                int ox = align switch
                {
                    "center" => b.W / 2,
                    "feet" => FeetCenter(mask),
                    "hang" => b.W / 2,
                    _ => BestAlignment(refMask, refOx, mask, oy),
                };
                if (align == "hang")
                    oy = 0;
                frames.Add(new Frame(b.X, b.Y, b.W, b.H, ox, oy, b.Row));
            }
            var arr = new JsonArray();
            foreach (Frame f in frames)
                arr.Add(new JsonArray(f.X, f.Y, f.W, f.H, f.Ox, f.Oy));
            var a = new JsonObject { ["frameTicks"] = (int?)node["frameTicks"] ?? 5, ["loop"] = (bool?)node["loop"] ?? false, ["frames"] = arr };
            outAnims[name] = a;
            if (previewDir != null)
                Preview(img, fg, frames, Path.Combine(previewDir, $"{Path.GetFileName(characterDir)}_{name}.png"));
        }
        output["animations"] = outAnims;
        File.WriteAllText(Path.Combine(characterDir, "frames.json"), Compact(output.ToJsonString(new JsonSerializerOptions { WriteIndented = true })));
        Console.WriteLine($"wrote {Path.Combine(characterDir, "frames.json")}");
    }

    //puts every frame array ([x, y, w, h, ox, oy]) on one line
    static string Compact(string json)
    {
        var lines = json.Replace("\r", "").Split('\n');
        var result = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (t == "[" && i + 6 < lines.Length && lines[i + 7].Trim().StartsWith("]"))
            {
                var nums = Enumerable.Range(i + 1, 6).Select(k => lines[k].Trim().TrimEnd(','));
                string indent = lines[i][..^1];
                result.Add(indent + "[" + string.Join(", ", nums) + lines[i + 7].Trim());
                i += 7;
                continue;
            }
            result.Add(lines[i].TrimEnd('\r'));
        }
        return string.Join("\n", result);
    }

    static bool[,] Mask(bool[] fg, int width, Box b)
    {
        var m = new bool[b.W, b.H];
        for (int y = 0; y < b.H; y++)
            for (int x = 0; x < b.W; x++)
                m[x, y] = fg[b.X + x + (b.Y + y) * width];
        return m;
    }

    //middle of the lowest fifth of the silhouette
    static int FeetCenter(bool[,] m)
    {
        int w = m.GetLength(0), h = m.GetLength(1);
        long sum = 0; int count = 0;
        for (int y = h - Math.Max(2, h / 5); y < h; y++)
            for (int x = 0; x < w; x++)
                if (m[x, y]) { sum += x; count++; }
        return count > 0 ? (int)(sum / count) : w / 2;
    }

    //the handle x that makes the frame overlap the reference frame the most (both standing on the same baseline)
    static int BestAlignment(bool[,] refMask, int refOx, bool[,] m, int oy)
    {
        int rw = refMask.GetLength(0), rh = refMask.GetLength(1);
        int w = m.GetLength(0), h = m.GetLength(1);
        int best = w / 2;
        double bestScore = -1;
        for (int ox = -rw; ox <= w + rw; ox++)
        {
            //frame pixel (x, y) lands on reference pixel (x - ox + refOx, y - oy + rh)
            int overlap = 0, frameCount = 0;
            for (int y = 0; y < h; y++)
            {
                int ry = y - oy + rh;
                for (int x = 0; x < w; x++)
                {
                    if (!m[x, y]) continue;
                    frameCount++;
                    int rx = x - ox + refOx;
                    if (rx >= 0 && rx < rw && ry >= 0 && ry < rh && refMask[rx, ry]) overlap++;
                }
            }
            double score = overlap - Math.Abs(ox - w / 2) * 0.01;
            if (score > bestScore)
            {
                bestScore = score;
                best = ox;
            }
        }
        return best;
    }

    //all frames of one animation drawn on top of each other at their handles, plus the frames side by side
    static void Preview(Img img, bool[] fg, List<Frame> frames, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        int left = frames.Max(f => f.Ox), right = frames.Max(f => f.W - f.Ox);
        int up = frames.Max(f => f.Oy), down = frames.Max(f => f.H - f.Oy);
        int cellW = left + right + 4, cellH = up + Math.Max(down, 0) + 4;
        const int s = 3;
        var o = new Img { W = cellW * (frames.Count + 1) * s, H = cellH * s, Rgba = new byte[cellW * (frames.Count + 1) * s * cellH * s * 4] };
        for (int i = 0; i < o.Rgba.Length; i += 4) { o.Rgba[i] = 40; o.Rgba[i + 1] = 40; o.Rgba[i + 2] = 40; o.Rgba[i + 3] = 255; }
        void Put(Frame f, int cell, int alpha)
        {
            int baseX = cell * cellW + 2 + left - f.Ox, baseY = 2 + up - f.Oy;
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    if (!fg[f.X + x + (f.Y + y) * img.W]) continue;
                    var p = img.Get(f.X + x, f.Y + y);
                    for (int py = 0; py < s; py++)
                        for (int px = 0; px < s; px++)
                        {
                            int ox = (baseX + x) * s + px, oy = (baseY + y) * s + py;
                            var q = o.Get(ox, oy);
                            o.Set(ox, oy, (p.r * alpha + q.r * (255 - alpha)) / 255, (p.g * alpha + q.g * (255 - alpha)) / 255, (p.b * alpha + q.b * (255 - alpha)) / 255);
                        }
                }
        }
        for (int i = 0; i < frames.Count; i++)
        {
            Put(frames[i], 0, 90);
            Put(frames[i], i + 1, 255);
        }
        //handle cross in every cell
        for (int c = 0; c <= frames.Count; c++)
            for (int k = -4; k <= 4; k++)
            {
                o.Set((c * cellW + 2 + left) * s + k, (2 + up) * s, 255, 0, 0);
                o.Set((c * cellW + 2 + left) * s, (2 + up) * s + k, 255, 0, 0);
            }
        Save(o, path);
    }
    #endregion
}
