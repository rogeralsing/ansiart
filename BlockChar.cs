using System;
using System.Linq;

namespace AsciiArt
{
    /**
 * 0000
 * 0000
 * 0000
 * 0000
 * 0000
 * 1111
 * 1111
 * 1111
 * 
 * Converts 4x8 RGB pixel to a unicode character and a foreground and background color:
 * Uses a variation of the median cut algorithm to determine a two-color palette for the
 * character, then creates a corresponding bitmap for the partial image covered by the
 * character and finds the best match in the character bitmap table.
*/
    internal class BlockChar
    {
        /**
     * Assumed bitmaps of the supported characters
     */
        private static readonly uint[] Bitmaps =
        {
            0x00000000, '\u00a0',

            // Block graphics

            // 0xffff0000, '\u2580',  // upper 1/2; redundant with inverse lower 1/2

            0x0000000f, '\u2581', // lower 1/8
            0x000000ff, '\u2582', // lower 1/4
            0x00000fff, '\u2583',
            0x0000ffff, '\u2584', // lower 1/2
            0x000fffff, '\u2585',
            0x00ffffff, '\u2586', // lower 3/4
            0x0fffffff, '\u2587',
            // 0xffffffff, '\u2588',  // full; redundant with inverse space

            0xeeeeeeee, '\u258a', // left 3/4
            0xcccccccc, '\u258c', // left 1/2
            0x88888888, '\u258e', // left 1/4

            0x0000cccc, '\u2596', // quadrant lower left
            0x00003333, '\u2597', // quadrant lower right
            0xcccc0000, '\u2598', // quadrant upper left
            // 0xccccffff, '\u2599',  // 3/4 redundant with inverse 1/4
            0xcccc3333, '\u259a', // diagonal 1/2
            // 0xffffcccc, '\u259b',  // 3/4 redundant
            // 0xffff3333, '\u259c',  // 3/4 redundant
            0x33330000, '\u259d', // quadrant upper right
            // 0x3333cccc, '\u259e',  // 3/4 redundant
            // 0x3333ffff, '\u259f',  // 3/4 redundant

            // Line drawing subset: no double lines, no complex light lines
            // Simple light lines duplicated because there is no center pixel int the 4x8 matrix

            0x000ff000, '\u2501', // Heavy horizontal
            0x66666666, '\u2503', // Heavy vertical

            0x00077666, '\u250f', // Heavy down and right
            0x000ee666, '\u2513', // Heavy down and left
            0x66677000, '\u2517', // Heavy up and right
            0x666ee000, '\u251b', // Heavy up and left

            0x66677666, '\u2523', // Heavy vertical and right
            0x666ee666, '\u252b', // Heavy vertical and left
            0x000ff666, '\u2533', // Heavy down and horizontal
            0x666ff000, '\u253b', // Heavy up and horizontal
            0x666ff666, '\u254b', // Heavy cross

            0x000cc000, '\u2578', // Bold horizontal left
            0x00066000, '\u2579', // Bold horizontal up
            0x00033000, '\u257a', // Bold horizontal right
            0x00066000, '\u257b', // Bold horizontal down

            0x06600660, '\u254f', // Heavy double dash vertical

            0x000f0000, '\u2500', // Light horizontal
            0x0000f000, '\u2500', //
            0x44444444, '\u2502', // Light vertical
            0x22222222, '\u2502',

            0x000e0000, '\u2574', // light left
            0x0000e000, '\u2574', // light left
            0x44440000, '\u2575', // light up
            0x22220000, '\u2575', // light up
            0x00030000, '\u2576', // light right
            0x00003000, '\u2576', // light right
            0x00004444, '\u2575', // light down
            0x00002222, '\u2575', // light down

            // Misc technical

            0x44444444, '\u23a2', // [ extension
            0x22222222, '\u23a5', // ] extension

            //12345678
            0x0f000000, '\u23ba', // Horizontal scanline 1
            0x00f00000, '\u23bb', // Horizontal scanline 3
            0x00000f00, '\u23bc', // Horizontal scanline 7
            0x000000f0, '\u23bd', // Horizontal scanline 9

            // Geometrical shapes. Tricky because some of them are too wide.

//      0x00ffff00, '\u25fe',  // Black medium small square
            0x00066000, '\u25aa', // Black small square


            0x11224488, '\u2571', // diagonals
            0x88442211, '\u2572',
            0x99666699, '\u2573',

            0x000137f0, '\u25e2', // Triangles
            0x0008cef0, '\u25e3',
            0x000fec80, '\u25e4',
            0x000f7310, '\u25e5'
        };

        /**
     * Maximum value for each color channel.
     */
        private readonly int[] _max = new int[3];

        /**
     * Minimum value for each color channel.
     */
        private readonly int[] _min = new int[3];

        /**
     * Red, green and blue components of the selected background color.
     */
        public int[] BgColor = new int[3];

        /**
     * Red, green and blue components of the selected background color.
     */
        public int[] FgColor = new int[3];

        /**
     * The selected character.
     */
        public char Character { get; private set; }

        /**
     * Converts a set of pixels to a unicode character and a background and foreground color.
     * data contains the rgba values, p0 is the start point in data and scanWidth the number
     * of bytes in each row of data.
     */
        public void Load(byte[] data, int p0, int scanWidth)
        {
            for (var i = 0; i < 3; i++)
            {
                _min[i] = 255;
                _max[i] = 0;
                BgColor[i] = 0;
                FgColor[i] = 0;
            }

            // Determine the minimum and maximum value for each color channel
            var pos = p0;
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var d = data[pos++] & 255;
                        _min[i] = Math.Min(_min[i], d);
                        _max[i] = Math.Max(_max[i], d);
                    }

                    pos++; // Alpha
                }

                pos += scanWidth - 16;
            }

            // Determine the color channel with the greatest range.
            var splitIndex = 0;
            var bestSplit = 0;
            for (var i = 0; i < 3; i++)
                if (_max[i] - _min[i] > bestSplit)
                {
                    bestSplit = _max[i] - _min[i];
                    splitIndex = i;
                }

            // We just split at the middle of the interval instead of computing the median.
            var splitValue = _min[splitIndex] + bestSplit / 2;

            // Compute a bitmap using the given split and sum the color values for both buckets.
            var bits = 0;
            var fgCount = 0;
            var bgCount = 0;

            pos = p0;
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    bits = bits << 1;
                    int[] avg;
                    if ((data[pos + splitIndex] & 255) > splitValue)
                    {
                        avg = FgColor;
                        bits |= 1;
                        fgCount++;
                    }
                    else
                    {
                        avg = BgColor;
                        bgCount++;
                    }

                    for (var i = 0; i < 3; i++) avg[i] += data[pos++] & 255;
                    pos++; // Alpha
                }

                pos += scanWidth - 16;
            }

            // Calculate the average color value for each bucket
            for (var i = 0; i < 3; i++)
            {
                if (bgCount != 0) BgColor[i] /= bgCount;
                if (fgCount != 0) FgColor[i] /= fgCount;
            }

            // Find the best bitmap match by counting the bits that don't match, including
            // the inverted bitmaps.
            var bestDiff = int.MaxValue;
            var invert = false;
            for (var i = 0; i < Bitmaps.Length; i += 2)
            {
                var b = Bitmaps[i];
                var diff = (b ^ bits).BitCount();
                if (diff < bestDiff)
                {
                    Character = (char) Bitmaps[i + 1];
                    bestDiff = diff;
                    invert = false;
                }

                diff = ((~b) ^ bits).BitCount();
                if (diff < bestDiff)
                {
                    Character = (char) Bitmaps[i + 1];
                    bestDiff = diff;
                    invert = true;
                }
            }

            // If the match is quite bad, use a shade image instead.
            // if (bestDiff > 10)
            // {
            //     invert = false;
            //     Character = " \u2591\u2592\u2593\u2588".ElementAt(Math.Min(4, fgCount * 5 / 32));
            // }

            // If we use an inverted character, we need to swap the colors.
            if (invert)
            {
                var tmp = BgColor;
                BgColor = FgColor;
                FgColor = tmp;
            }
        }
    }
}