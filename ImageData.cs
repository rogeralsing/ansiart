using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AsciiArt
{
    internal class ImageData
    {
        private readonly byte[] _data;
        private readonly int _height;
        private readonly int _width;


        public ImageData(Image<Rgba32> image)
        {
            var size = image.Size();
            _width = size.Width;
            _height = size.Height;
            _data = new byte[_width * _height * 4];
            for (var y = 0; y < _height; y++)
            {
                var pixels = image.GetPixelRowSpan(y);
                for (var x = 0; x < _width; x++)
                {
                    var i = x * 4 + _width * 4 * y;
                    var pixel = pixels[x];
                    _data[i + 0] = pixel.R;
                    _data[i + 1] = pixel.G;
                    _data[i + 2] = pixel.B;
                    _data[i + 3] = pixel.A;
                }
            }
        }
        
        public ImageData(Size size)
        { 
            _width = size.Width;
            _height = size.Height;
            _data = new byte[_width * _height * 4];
        }
        
        public void Load(ImageFrame<Rgba32> image)
        {
            for (var y = 0; y < _height; y++)
            {
                var pixels = image.GetPixelRowSpan(y);
                for (var x = 0; x < _width; x++)
                {
                    var i = x * 4 + _width * 4 * y;
                    var pixel = pixels[x];
                    _data[i + 0] = pixel.R;
                    _data[i + 1] = pixel.G;
                    _data[i + 2] = pixel.B;
                    _data[i + 3] = pixel.A;
                }
            }
        }

        /**
     * Convert the image to an Ansi control character string setting the colors
     */
        public string Render(Mode mode)
        {
            var sb = new StringBuilder();
            var blockChar = new BlockChar();

            for (var y = 0; y < _height - 7; y += 8)
            {
                var pos = y * _width * 4;

                var lastFg = "";
                var lastBg = "";
                for (var x = 0; x < _width - 3; x += 4)
                {
                    blockChar.Load(_data, pos, _width * 4);
                    var fg = Ansi.Color(Mode.Fg | mode, blockChar.FgColor[0], blockChar.FgColor[1],
                        blockChar.FgColor[2]);
                    var bg = Ansi.Color(Mode.Bg | mode, blockChar.BgColor[0], blockChar.BgColor[1],
                        blockChar.BgColor[2]);
                    if (fg != lastFg)
                    {
                        sb.Append(fg);
                        lastFg = fg;
                    }

                    if (bg != lastBg)
                    {
                        sb.Append(bg);
                        lastBg = bg;
                    }

                    sb.Append(blockChar.Character);
                    pos += 16;
                }

                sb.Append(Ansi.Reset).Append('\n');
            }

            return sb.ToString();
        }
    }
}