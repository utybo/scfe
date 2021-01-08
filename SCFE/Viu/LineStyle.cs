/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
namespace Viu
{
    public class LineStyle
    {
        public static readonly LineStyle Barebones = new LineStyle('-', '|', '+', '+', '+', '+');
        public static readonly LineStyle Simple = new LineStyle('─', '│', '┌', '┐', '└', '┘');
        public static readonly LineStyle Dotted = new LineStyle('┅', '┇', '┌', '┐', '└', '┘');

        public LineStyle(char horizontal, char vertical, char topLeft, char topRight, char bottomLeft, char bottomRight)
        {
            Horizontal = horizontal;
            Vertical = vertical;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public char Horizontal { get; }
        public char Vertical { get; }
        public char TopLeft { get; }
        public char TopRight { get; }
        public char BottomLeft { get; }
        public char BottomRight { get; }
    }
}
