/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using Viu.Components;

namespace Viu.Strategy
{
    /// <summary>
    ///     A LayoutStrategy that organizes its components like so:
    ///     <pre>
    ///         <code>
    /// +------------------------+
    /// |          TOP           |
    /// +------------------------+
    /// |      |         |       |
    /// | LEFT |  CENTER | RIGHT |
    /// |      |         |       |
    /// +------------------------+
    /// |         BOTTOM         |
    /// +------------------------+
    /// </code>
    ///     </pre>
    ///     This strategy REQUIRES a string when you in Parent.AddComponent. Use the constants provided in this class.
    /// </summary>
    public class BorderStrategy : LayoutStrategy
    {
        public const string Top = "top", Center = "center", Left = "left", Right = "right", Bottom = "bottom";

        public override void ApplyLayoutStrategy(Parent p)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, p);


            int topH = 0, botH = 0;
            int leftW = 0, rightW = 0;

            if (top != null)
            {
                top.X = p.X;
                top.Y = p.Y;
                top.Width = p.Width;
                var d = top.ComputeDimensions();
                topH = top.Height = p.PrefHeight == Component.Computed ? d.Height : p.PrefHeight;
            }

            if (bottom != null)
            {
                bottom.X = p.X;
                var d = bottom.ComputeDimensions();
                botH = bottom.Height =
                    bottom.PrefHeight == Component.Computed ? d.Height : bottom.PrefHeight;
                bottom.Y = p.Y + p.Height - bottom.Height;
                bottom.Width = p.Width;
            }

            if (left != null)
            {
                left.X = p.X;
                left.Y = p.Y + topH;
                var leftD = left.ComputeDimensions();
                leftW = left.Width = left.PrefWidth == Component.Computed ? leftD.Width : left.PrefWidth;
                left.Height = p.Height - topH - botH;
            }

            if (right != null)
            {
                right.Y = p.Y + topH;
                var rightD = right.ComputeDimensions();
                rightW = right.Width = right.PrefWidth == Component.Computed ? rightD.Width : right.PrefWidth;
                right.X = p.X + p.Width - right.Width;
                right.Height = p.Height - topH - botH;
            }

            if (center != null)
            {
                center.X = p.X + leftW;
                center.Y = p.Y + topH;
                center.Width = p.Width - leftW - rightW;
                center.Height = p.Height - topH - botH;
            }
        }

        public override Dimensions ComputeDimensions(Parent p)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, p);

            var w = 0;
            var h = 0;
            if (left != null)
                w += left.ComputeDimensions().Width;
            if (right != null)
                w += right.ComputeDimensions().Width;
            if (top != null)
                h += top.ComputeDimensions().Height;
            if (bottom != null)
                h += bottom.ComputeDimensions().Height;
            if (center != null)
            {
                var d = center.ComputeDimensions();
                h += d.Height;
                w += d.Width;
            }

            return new Dimensions(w, h);
        }

        private void DetermineComponents(out Component left, out Component center, out Component right,
            out Component top, out Component bottom, Parent p)
        {
            left = null;
            center = null;
            right = null;
            top = null;
            bottom = null;
            foreach (var c in p.GetChildren())
            {
                if (!c.Visible) continue;
                var spot = c.LayoutInformation as string;
                switch (spot)
                {
                    case Center:
                        center = c;
                        break;
                    case Left:
                        left = c;
                        break;
                    case Right:
                        right = c;
                        break;
                    case Top:
                        top = c;
                        break;
                    case Bottom:
                        bottom = c;
                        break;
                }
            }
        }

        private void DetermineAvailability(out bool leftAvailable, out bool centerAvailable, out bool rightAvailable,
            out bool bottomAvailable, out bool topAvailable, Component left, Component center, Component right,
            Component top, Component bottom)
        {
            leftAvailable = (left as IFocusable)?.IsFocusable() ?? false;
            centerAvailable = (center as IFocusable)?.IsFocusable() ?? false;
            rightAvailable = (right as IFocusable)?.IsFocusable() ?? false;
            bottomAvailable = (bottom as IFocusable)?.IsFocusable() ?? false;
            topAvailable = (top as IFocusable)?.IsFocusable() ?? false;
        }

        public override bool IsHintAllowed(object hint)
        {
            var s = hint as string;
            return s != null && (s == Top || s == Bottom || s == Center || s == Left || s == Right);
        }

        public override IFocusable GetPreviousFocusableElement(Parent parent, Component previousFocus)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, parent);

            bool leftAvailable, centerAvailable, rightAvailable, bottomAvailable, topAvailable;
            DetermineAvailability(out leftAvailable, out centerAvailable, out rightAvailable, out bottomAvailable,
                out topAvailable, left, center, right, top, bottom);

            if (previousFocus == null)
            {
                if (rightAvailable)
                    return (IFocusable) right;
                if (centerAvailable)
                    return (IFocusable) center;
                if (bottomAvailable)
                    return (IFocusable) bottom;
                if (leftAvailable)
                    return (IFocusable) left;
                if (topAvailable)
                    return (IFocusable) top;
            }
            else
            {
                if (previousFocus == center && leftAvailable)
                    return (IFocusable) left;
                if (previousFocus == right && centerAvailable)
                    return (IFocusable) center;
                if (previousFocus == right && leftAvailable)
                    return (IFocusable) left;
                if (previousFocus == bottom)
                    return leftAvailable ? (IFocusable) left :
                        centerAvailable ? (IFocusable) center :
                        rightAvailable ? (IFocusable) right :
                        topAvailable ? (IFocusable) top :
                        null;
                if (previousFocus == top && leftAvailable)
                    return (IFocusable) left;
            }

            return null;
        }

        public override IFocusable GetNextFocusableElement(Parent parent, Component previousFocus)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, parent);

            bool leftAvailable, centerAvailable, rightAvailable, bottomAvailable, topAvailable;
            DetermineAvailability(out leftAvailable, out centerAvailable, out rightAvailable, out bottomAvailable,
                out topAvailable, left, center, right, top, bottom);

            if (previousFocus == null)
            {
                if (leftAvailable)
                    return (IFocusable) left;
                if (centerAvailable)
                    return (IFocusable) center;
                if (topAvailable)
                    return (IFocusable) top;
                if (rightAvailable)
                    return (IFocusable) right;
                if (bottomAvailable)
                    return (IFocusable) bottom;
            }
            else
            {
                if (previousFocus == center && rightAvailable)
                    return (IFocusable) right;
                if (previousFocus == left && centerAvailable)
                    return (IFocusable) center;
                if (previousFocus == left && rightAvailable)
                    return (IFocusable) right;
                if (previousFocus == top)
                    return rightAvailable ? (IFocusable) right :
                        centerAvailable ? (IFocusable) center :
                        leftAvailable ? (IFocusable) left :
                        bottomAvailable ? (IFocusable) bottom :
                        null;
                if (previousFocus == bottom && rightAvailable)
                    return (IFocusable) right;
            }

            return null;
        }

        public override IFocusable GetDownFocusableElement(Parent parent, Component previousFocus)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, parent);

            bool leftAvailable, centerAvailable, rightAvailable, bottomAvailable, topAvailable;
            DetermineAvailability(out leftAvailable, out centerAvailable, out rightAvailable, out bottomAvailable,
                out topAvailable, left, center, right, top, bottom);

            if (previousFocus == null)
            {
                if (topAvailable)
                    return (IFocusable) top;
                if (centerAvailable)
                    return (IFocusable) center;
                if (leftAvailable)
                    return (IFocusable) left;
                if (rightAvailable)
                    return (IFocusable) right;
                if (bottomAvailable)
                    return (IFocusable) bottom;
            }
            else
            {
                if (previousFocus == top)
                    return centerAvailable ? (IFocusable) center :
                        leftAvailable ? (IFocusable) left :
                        rightAvailable ? (IFocusable) right :
                        null;
                if ((previousFocus == center || previousFocus == left || previousFocus == right) && bottomAvailable)
                    return (IFocusable) bottom;
            }

            return null;
        }

        public override IFocusable GetUpFocusableElement(Parent parent, Component previousFocus)
        {
            Component left, center, right, top, bottom;
            DetermineComponents(out left, out center, out right, out top, out bottom, parent);
            bool leftAvailable, centerAvailable, rightAvailable, bottomAvailable, topAvailable;
            DetermineAvailability(out leftAvailable, out centerAvailable, out rightAvailable, out bottomAvailable,
                out topAvailable, left, center, right, top, bottom);


            if (previousFocus == null)
            {
                if (topAvailable)
                    return (IFocusable) top;
                if (centerAvailable)
                    return (IFocusable) center;
                if (leftAvailable)
                    return (IFocusable) left;
                if (rightAvailable)
                    return (IFocusable) right;
                if (bottomAvailable)
                    return (IFocusable) bottom;
            }
            else
            {
                if (previousFocus == bottom)
                    return centerAvailable ? (IFocusable) center :
                        leftAvailable ? (IFocusable) left :
                        rightAvailable ? (IFocusable) right : null;
                if ((previousFocus == center || previousFocus == left || previousFocus == right) && topAvailable)
                    return (IFocusable) top;
            }

            return null;
        }
    }
}
