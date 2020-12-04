using System;

namespace AsciiArt
{
    [Flags]
    public enum Mode
    {
        Fg = 1,
        Bg = 2,
        Mode256 = 4,
        Mode24Bit = 8
    }

    /**
   * ANSI control code helpers
   */
    internal static class Ansi
    {
        private static readonly int[] ColorSteps = {0, 0x5f, 0x87, 0xaf, 0xd7, 0xff};

        private static readonly int[] Grayscale =
        {
            0x08, 0x12, 0x1c, 0x26, 0x30, 0x3a, 0x44, 0x4e, 0x58, 0x62, 0x6c, 0x76,
            0x80, 0x8a, 0x94, 0x9e, 0xa8, 0xb2, 0xbc, 0xc6, 0xd0, 0xda, 0xe4, 0xee
        };

        public static readonly string Reset = "\u001b[0m";

        private static int BestIndex(int v, int[] options)
        {
            var index = Array.BinarySearch(options, v);
            if (index < 0)
            {
                index = -index - 1;
                // need to check [index] and [index - 1]
                if (index == options.Length)
                {
                    index = options.Length - 1;
                }
                else if (index > 0)
                {
                    var val0 = options[index - 1];
                    var val1 = options[index];
                    if (v - val0 < val1 - v) index = index - 1;
                }
            }

            return index;
        }

        private static int Sqr(int i)
        {
            return i * i;
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static string Color(Mode flags, int r, int g, int b)
        {
            r = Clamp(r, 0, 255);
            g = Clamp(g, 0, 255);
            b = Clamp(b, 0, 255);

            var bg = (flags & Mode.Bg) != 0;

            if ((flags & Mode.Mode256) == 0) return $"{(bg ? "\u001b[48;2;" : "\u001b[38;2;")}{r};{g};{b}m";
            var rIdx = BestIndex(r, ColorSteps);
            var gIdx = BestIndex(g, ColorSteps);
            var bIdx = BestIndex(b, ColorSteps);

            var rQ = ColorSteps[rIdx];
            var gQ = ColorSteps[gIdx];
            var bQ = ColorSteps[bIdx];

            var gray = (int) Math.Round(r * 0.2989f + g * 0.5870f + b * 0.1140f);

            var grayIdx = BestIndex(gray, Grayscale);
            var grayQ = Grayscale[grayIdx];

            int colorIndex;
            if (0.3 * Sqr(rQ - r) + 0.59 * Sqr(gQ - g) + 0.11 * Sqr(bQ - b) <
                0.3 * Sqr(grayQ - r) + 0.59 * Sqr(grayQ - g) + 0.11 * Sqr(grayQ - b))
                colorIndex = 16 + 36 * rIdx + 6 * gIdx + bIdx;
            else
                colorIndex = 232 + grayIdx; // 1..24 -> 232..255
            return (bg ? "\u001B[48;5;" : "\u001B[38;5;") + colorIndex + "m";
        }
    }
}