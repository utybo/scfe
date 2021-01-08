/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Text;

namespace Viu
{
    /// <summary>
    ///     An implementation of GraphicsContext that uses the basic Console class
    /// </summary>
    public class ConsoleGraphicsContext : GraphicsContext
    {
        private ConsoleColor _defaultFg, _defaultBg;

        public override ConsoleColor DefaultBackground => _defaultBg;

        public override ConsoleColor DefaultForeground => _defaultFg;

        public override ConsoleColor CurrentForeground
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public override ConsoleColor CurrentBackground
        {
            get => Console.BackgroundColor;
            set => Console.BackgroundColor = value;
        }


        public override void Initialize()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            Console.TreatControlCAsInput = true;
            _defaultFg = Console.ForegroundColor;
            _defaultBg = Console.BackgroundColor;
            if ((int) Console.ForegroundColor == -1 || (int) Console.BackgroundColor == -1)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }


        public override void Write(int x, int y, string s)
        {
            EnsureOrFixSizing(ref x, ref y);
            Console.SetCursorPosition(x, y);
            Console.Write(s);
            RealignIfNecessary();
        }

        /// <summary>
        ///     Realign the cursor to the top of the console if it is below the window. This is to avoid the console
        ///     scrolling down.
        /// </summary>
        private void RealignIfNecessary()
        {
            if (Console.CursorTop >= Console.WindowHeight)
                Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        ///     Method used to avoid crashes on rapid window size changes. Makes sure the X and Y values are within the
        ///     bounds accepted by the Console class.
        /// </summary>
        /// <param name="x">Reference to the x value</param>
        /// <param name="y">Reference to the y value</param>
        private void EnsureOrFixSizing(ref int x, ref int y)
        {
            if (x >= Console.WindowWidth)
                x = Console.WindowWidth - 1;
            if (x < 0)
                x = 0;
            if (y >= Console.WindowHeight)
                y = Console.WindowHeight - 1;
            if (y < 0)
                y = 0;
        }

        /// <summary>
        ///     Write a string at the given x and y coordinates, reversing the color scheme (the background is used as the
        ///     foreground color, the foreground is used as the background). Usually used to show that an element is in
        ///     focus.
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="s">The string to display</param>
        public override void WriteRevert(int x, int y, string s)
        {
            EnsureOrFixSizing(ref x, ref y);
            Console.SetCursorPosition(x, y);
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;
            Console.ForegroundColor = bg;
            Console.BackgroundColor = fg;
            Console.Write(s);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            RealignIfNecessary();
        }

        /// <summary>
        ///     Write a string at the given x and y coordinates, reversing the color scheme (the background is used as the
        ///     foreground color, the foreground is used as the background). Usually used to show that an element is in
        ///     focus.
        ///     This overload allows you to provide a custom foreground color (which will be used as the background color)
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="s">The string to write</param>
        /// <param name="normallyForeground">
        ///     The color that is normally used as the foreground. It will be used as the
        ///     background color for the reverted color scheme.
        /// </param>
        public override void WriteRevert(int x, int y, string s, ConsoleColor normallyForeground)
        {
            EnsureOrFixSizing(ref x, ref y);
            Console.SetCursorPosition(x, y);
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;
            Console.ForegroundColor = bg;
            Console.BackgroundColor = normallyForeground;
            Console.Write(s);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            RealignIfNecessary();
        }


        public override void ClearArea(int startX, int startY, int width, int height)
        {
            for (var y = startY; y < startY + height; y++) Write(startX, y, new string(' ', width));
            RealignIfNecessary();
        }

        public override void SwapColors()
        {
            ConsoleColor fg = Console.ForegroundColor, bg = Console.BackgroundColor;
            Console.ForegroundColor = bg;
            Console.BackgroundColor = fg;
        }

        public override void SetCursorPosition(int x, int y)
        {
            Console.SetCursorPosition(x, y);
        }

        public override void SetCursorVisible(bool visibility)
        {
            Console.CursorVisible = visibility;
        }

        public override void Clear()
        {
            Console.Clear();
            Console.CursorVisible = true;
        }

        /// <summary>
        ///     Write a string to the console at the given x and y coordinates, in the given given foreground color. The
        ///     previous foreground color is remembered and restored.
        /// </summary>
        /// <param name="x">x position to start the string (x=0 is the left side)</param>
        /// <param name="y">y position to start the string (y=0 is the top side)</param>
        /// <param name="s">The string to write</param>
        /// <param name="foreground">The foreground color to use, or null to not use any.</param>
        /// <param name="background">The background color to use, or null to not use any.</param>
        public override void Write(int x, int y, string s, ConsoleColor? foreground, ConsoleColor? background)
        {
            EnsureOrFixSizing(ref x, ref y);
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;
            // Use the given color if it's not null, if it's null just use the regular color
            Console.ForegroundColor = foreground ?? Console.ForegroundColor;
            Console.BackgroundColor = background ?? Console.BackgroundColor;
            Console.SetCursorPosition(x, y);
            Console.Write(s);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            RealignIfNecessary();
        }
    }
}
