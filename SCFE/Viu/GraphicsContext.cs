/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using JetBrains.Annotations;

namespace Viu
{
    /// <summary>
    ///     A Graphics Context provides all of the necessary basic write operations to the display.
    /// </summary>
    public abstract class GraphicsContext
    {
        /// <summary>
        ///     The default background used by the GraphicsContext. It is determined when the graphics context is
        ///     initialized.
        /// </summary>
        public abstract ConsoleColor DefaultBackground { get; }

        /// <summary>
        ///     The default foreground used by the GraphicsContext. It is determined when the graphics context is
        ///     initialized.
        /// </summary>
        public abstract ConsoleColor DefaultForeground { get; }

        /// <summary>
        ///     The current foreground used by the GraphicsContext. It is used in all the methods that do not provide a way
        ///     to customize the colors, and is used when "null" is used as a parameter when a method allows you to define
        ///     custom colors.
        /// </summary>
        public abstract ConsoleColor CurrentForeground { get; set; }

        /// <summary>
        ///     The current background used by the GraphicsContext. It is used in all the methods that do not provide a way
        ///     to customize the colors, and is used when "null" is used as a parameter when a method allows you to define
        ///     custom colors.
        /// </summary>
        public abstract ConsoleColor CurrentBackground { get; set; }

        /// <summary>
        ///     Initialize the various flags required for the GraphicsContext to work. This is called by the root
        ///     parent and is only called once throughout the lifecycle of the application.
        ///     This is where the default background and foreground colors should be determined.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        ///     Write a string to the console, starting at the given (x,y) coordinates.
        /// </summary>
        /// <param name="x">x coordinate for the first character of the string (x=0 corresponds to the left)</param>
        /// <param name="y">y coordinate for the first character of the string (y=0 corresponds to the top)</param>
        /// <param name="s">The string to write</param>
        public abstract void Write(int x, int y, [NotNull] string s);

        /// <summary>
        ///     Write a string to the console, starting at the given (x,y) coordinates and using the given colors
        /// </summary>
        /// <param name="x">x coordinate for the first character of the string (x=0 corresponds to the left)</param>
        /// <param name="y">y coordinate for the first character of the string (x=0 corresponds to the top)</param>
        /// <param name="s">The string to write</param>
        /// <param name="foreground">The foreground color to use, or null to use the current one</param>
        /// <param name="background">The background color to use, or null to use the current one</param>
        public abstract void Write(int x, int y, [NotNull] string s,
            [CanBeNull] ConsoleColor? foreground,
            [CanBeNull] ConsoleColor? background);

        /// <summary>
        ///     Write a string to the console, starting at the given (x,y) coordinates with reversed colors: the background
        ///     color is the foreground color and vice versa. This does not actually change the colors in memory and only
        ///     writes the string with swapped colors.
        /// </summary>
        /// <param name="x">x coordinate for the first character of the string (x=0 corresponds to the left)</param>
        /// <param name="y">y coordinate for the first character of the string (x=0 corresponds to the top)</param>
        /// <param name="s">The string to write</param>
        public abstract void WriteRevert(int x, int y, [NotNull] string s);

        /// <summary>
        ///     Write a string to the console, starting at the given (x,y) coordinates with reversed colors using a custom color
        ///     that would normally be in the foreground (and will thus be used as the background color). The foreground color used
        ///     will be the current background color. This does not actually change the colors in memory and only
        ///     writes the string with swapped colors.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="s"></param>
        /// <param name="normallyForeground"></param>
        public abstract void WriteRevert(int x, int y, [NotNull] string s,
            ConsoleColor normallyForeground);

        /// <summary>
        ///     Clear an area in the console, emptying it. The current background will be used for the
        ///     clearing. The coordinates define the top left coordinate of the rectangle to clear, its width and its height
        /// </summary>
        /// <param name="startX">The top-left X coordinate of the rectangle to clear</param>
        /// <param name="startY">The top-left Y coordinate of the rectangle to clear</param>
        /// <param name="width">The width of the rectangle to clear</param>
        /// <param name="height">The height of the rectangle to clear</param>
        public abstract void ClearArea(int startX, int startY, int width,
            int height);

        /// <summary>
        ///     Swap the current foreground color with the current background color in memory.
        /// </summary>
        public abstract void SwapColors();

        /// <summary>
        ///     Set the position of the cursor to the given x and y coordinates. This should not be used as a way to
        ///     position the cursor when writing things, but should instead be used to show the cursor for components
        ///     that require some way to show a blinking cursor (e.g. TextField)
        /// </summary>
        /// <param name="x">The x position where to place the cursor (x=0 corresponds to the left)</param>
        /// <param name="y">The y position where to pace the cursor (y=0 corresponds to the top)</param>
        public abstract void SetCursorPosition(int x, int y);

        /// <summary>
        ///     Set whether the cursor should be visible or not. It will be displayed at the x and y coordinates set by
        ///     SetCursorPosition. The position of the cursor will shift if any of the Write methods are used: it is recommended to
        ///     set the cursor's visibility to false in order to avoid it being shown jumping around.
        /// </summary>
        /// <param name="visibility">Whether the cursor should be visible (true) or not (false)</param>
        public abstract void SetCursorVisible(bool visibility);

        /// <summary>
        ///     Clear the entire console's display.
        /// </summary>
        public abstract void Clear();
    }
}
