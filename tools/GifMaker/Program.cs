using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

//GifMaker <frames folder> <output.gif> [width] [frames per second]
//joins frame0000.png, frame0001.png, ... into a looping animated GIF
if (args.Length < 2)
{
    Console.WriteLine("usage: GifMaker <frames folder> <output.gif> [width=960] [fps=15]");
    return 1;
}

string[] files = Directory.GetFiles(args[0], "frame*.png").OrderBy(f => f).ToArray();
int width = args.Length > 2 ? int.Parse(args[2]) : 960;
int fps = args.Length > 3 ? int.Parse(args[3]) : 15;
//GIF delays are in hundredths of a second
int delay = (int)Math.Round(100.0 / fps);

using var gif = Image.Load<Rgba32>(files[0]);
Resize(gif);
gif.Metadata.GetGifMetadata().RepeatCount = 0;
gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = delay;

foreach (string file in files.Skip(1))
{
    using var frame = Image.Load<Rgba32>(file);
    Resize(frame);
    frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = delay;
    gif.Frames.AddFrame(frame.Frames.RootFrame);
}

gif.SaveAsGif(args[1], new GifEncoder { ColorTableMode = GifColorTableMode.Local });
Console.WriteLine($"wrote {args[1]}: {files.Length} frames, {new FileInfo(args[1]).Length / 1024} KB");
return 0;

//pixel art is scaled with nearest neighbour so it stays sharp
void Resize(Image image)
{
    int height = image.Height * width / image.Width;
    image.Mutate(x => x.Resize(width, height, KnownResamplers.NearestNeighbor));
}
