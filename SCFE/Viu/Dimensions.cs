/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using Viu.Components;

namespace Viu
{
    public struct Dimensions
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Dimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public Dimensions Add(int addW, int addH)
        {
            return new Dimensions(addW + Width, addH + Height);
        }

        public static Dimensions DimensionsOf(Component c)
        {
            return new Dimensions(c.Width, c.Height);
        }
    }
}
