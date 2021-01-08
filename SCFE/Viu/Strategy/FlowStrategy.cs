/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using Viu.Components;

namespace Viu.Strategy
{
    public class FlowStrategy : LayoutStrategy
    {
        public FlowStrategy()
        {
        }

        public FlowStrategy(bool wrap)
        {
            Wrapping = wrap;
        }

        public FlowStrategy(int hGap)
        {
            HGap = hGap;
        }

        public FlowStrategy(bool wrap, int hGap, int vGap)
        {
            Wrapping = wrap;
            HGap = hGap;
            VGap = vGap;
        }

        private bool Wrapping { get; }

        public int HGap { get; set; }

        public int VGap { get; set; }

        public override void ApplyLayoutStrategy(Parent p)
        {
            var nextX = p.X;
            var nextY = p.Y;
            var maxY = 0; // Used for properly wrapping elements

            foreach (var c in p.GetChildren())
            {
                var d = c.ComputeDimensions();
                c.Width = c.PrefWidth == -1 ? d.Width : c.PrefWidth;
                c.Height = c.PrefHeight == -1 ? d.Height : c.PrefHeight;

                if (c.Width + nextX > p.X + p.Width)
                {
                    if (nextX == p.X)
                    {
                        c.Width = p.Width;
                    }
                    else
                    {
                        if (Wrapping)
                        {
                            nextY += maxY + VGap;
                            nextX = p.X;
                            maxY = 0;
                        }
                        else
                        {
                            if (nextX < p.Width)
                                c.Width = p.Width - nextX;
                            else
                                c.Width = 0;
                        }
                    }
                }

                c.X = nextX;
                c.Y = nextY;
                // TODO Care about min and max values

                if (c.Height > maxY)
                    maxY = c.Height;

                nextX += c.Width + HGap;
            }
        }

        public override Dimensions ComputeDimensions(Parent p)
        {
            var totalH = 0;
            var totalW = 0;

            var rowH = 0;
            var rowW = 0;

            foreach (var c in p.GetChildren())
            {
                var d = c.ComputeDimensions();
                var wi = c.PrefWidth == -1 ? d.Width : c.PrefWidth;
                var he = c.PrefHeight == -1 ? d.Height : c.PrefHeight;

                if (Wrapping && rowW > 0 && rowW + wi > p.Width)
                {
                    totalH += rowH + VGap;
                    totalW = Math.Max(rowW - HGap, totalW);

                    rowH = he;
                    rowW = wi + HGap;
                }
                else
                {
                    rowH = Math.Max(he, rowH);
                    rowW += wi + HGap;
                }
            }

            return new Dimensions(Math.Max(totalW, rowW - HGap), totalH + rowH);
        }

        public override IFocusable GetNextFocusableElement(Parent parent, Component previousFocus)
        {
            var components = parent.GetChildren();
            for (var i = components.IndexOf(previousFocus) + 1; i < components.Count; i++)
                if ((components[i] as IFocusable)?.IsFocusable() ?? false)
                    return (IFocusable) components[i];

            return null;
        }

        public override IFocusable GetPreviousFocusableElement(Parent parent, Component c)
        {
            var components = parent.GetChildren();
            for (var i = components.IndexOf(c) - 1; i >= 0; i--)
                if ((components[i] as IFocusable)?.IsFocusable() ?? false)
                    return (IFocusable) components[i];

            return null;
        }

        public override bool IsHintAllowed(object hint)
        {
            return hint == null || "" == hint as string;
        }
    }
}
