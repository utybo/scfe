/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;

namespace Viu.Components
{
    public class Separator : Component
    {
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public LineStyle Style { get; set; } = LineStyle.Simple;
        public ConsoleColor? Foreground { get; set; } = null;

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;
            if (Orientation == Orientation.Vertical)
            {
                var ch = Style.Vertical;
                for (var y = Y; y < Y + Height; y++) g.Write(X, y, ch + "", Foreground, null);
            }
            else
            {
                var ch = Style.Horizontal;
                g.Write(X, Y, new string(ch, Width), Foreground, null);
            }
        }

        public override Dimensions ComputeDimensions()
        {
            return new Dimensions(1, 1);
        }
    }
}
