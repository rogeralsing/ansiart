using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SearchOption = Microsoft.VisualBasic.FileIO.SearchOption;

namespace AsciiArt
{

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            
            var arg = string.Join("", args);
            if (arg == "")
            {
                arg = ".";
            }
            
            if (arg.ToLowerInvariant().StartsWith("http"))
            {
                await RenderWebPage(arg);
            }
            else if (Directory.Exists(arg))
            {
                RenderDirectory(arg);
            }
            else
            {
                RenderImage(arg,true);
            }
        }

        private static async Task RenderWebPage(string arg)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync(arg);
            await page.ScreenshotAsync("tmp.png");
            RenderImage("tmp.png", false);
        }

        private static void RenderDirectory(string arg)
        {
            var extensions = new[] {".gif", ".png", ".jpg", ".jpeg",".bmp",".tga"};
            var files = Directory.GetFiles(arg,"*.*",System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (!extensions.Contains(ext)) continue;

                RenderImage(file, false);
            }
        }

        private static void RenderImage(string path, bool animate)
        {
            using var img = (Image<Rgba32>) Image.Load(path);

            var size = img.Size();

            if (size.Width > 320 || size.Height > 600)
            {
                img.Mutate(c => c.Resize(new ResizeOptions()
                {
                    Mode = ResizeMode.Max,
                    Size = new Size() {Width = 320, Height = 600}
                }));
            }

            Console.WriteLine(path);
            
            Console.WriteLine(size);

            if (img.Frames.Count == 1 || !animate)
            {
                var id1 = new ImageData(img);
                var i1 = id1.Render(Mode.Mode24Bit);
                Console.WriteLine(i1);
            }
            else
            {
                var i = 0;
                var id = new ImageData(img.Size());
                var frames = new List<string>();

                foreach (ImageFrame<Rgba32> f in img.Frames)
                {
                    id.Load(f);
                    var frame = id.Render(Mode.Mode24Bit);
                    frames.Add(frame);
                    i++;
                }

                var delay = img.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay * 10;
                Console.Clear();
                bool run = true;
                Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs eventArgs) { run = false; };
                while (run)
                {
                    foreach (var frame in frames)
                    {
                        var sw = Stopwatch.StartNew();
                        Console.SetCursorPosition(0, 0);
                        Console.WriteLine(frame);
                        var renderms = (int) sw.Elapsed.TotalMilliseconds;
                        var timeleft = delay - renderms;
                        if (timeleft > 0) Thread.Sleep(timeleft);
                    }
                }
            }
        }
    }
}