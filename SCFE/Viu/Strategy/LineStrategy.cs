/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System.Linq;
using Viu.Components;

namespace Viu.Strategy
{
    public class LineStrategy : LayoutStrategy
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        public bool Centered { get; set; } = false;
        public int Gap { get; set; }

        public override void ApplyLayoutStrategy(Parent p)
        {
            var children = p.GetChildren().Where(i => i.Visible).ToList();

            int req;
            if (Orientation == Orientation.Vertical)
            {
                children.ForEach(c => c.Width = p.Width);
                req = children
                          .Select(i => i.PrefHeight == Component.Computed ? i.ComputeDimensions().Height : i.PrefHeight)
                          .Sum()
                      + Gap * (children.Count - 1);
            }
            else
            {
                children.ForEach(c => c.Height = p.Height);
                req = children
                          .Select(i => i.PrefWidth == Component.Computed ? i.ComputeDimensions().Width : i.PrefWidth)
                          .Sum()
                      + Gap * (children.Count - 1);
            }

            var x = Orientation == Orientation.Horizontal && Centered ? p.X + (p.Width - req) / 2 : p.X;
            var y = Orientation == Orientation.Vertical && Centered ? p.Y + (p.Height - req) / 2 : p.Y;

            foreach (var c in children)
            {
                c.X = x;
                c.Y = y;
                var dim = c.ComputeDimensions();
                if (Orientation == Orientation.Vertical)
                {
                    c.Height = c.PrefHeight == Component.Computed ? dim.Height : c.PrefHeight;
                    y += c.Height + Gap;
                }
                else if (Orientation == Orientation.Horizontal)
                {
                    c.Width = c.PrefWidth == Component.Computed ? dim.Height : c.PrefHeight;
                    x += c.Width + Gap;
                }
            }
        }

        public override Dimensions ComputeDimensions(Parent p)
        {
            int wi = 0, he = 0;
            var children = p.GetChildren().Where(i => i.Visible).ToList();
            if (Orientation == Orientation.Vertical)
            {
                wi = p.Width;

                he = Centered
                    ? p.Height
                    : children
                          .Select(i => i.PrefHeight == Component.Computed ? i.ComputeDimensions().Height : i.PrefHeight)
                          .Sum()
                      + Gap * (children.Count - 1);
            }

            if (Orientation == Orientation.Horizontal)
            {
                he = p.Height;

                wi = Centered
                    ? p.Width
                    : children
                          .Select(i => i.PrefWidth == Component.Computed ? i.ComputeDimensions().Width : i.PrefWidth)
                          .Sum()
                      + Gap * (children.Count - 1);
            }

            return new Dimensions(wi, he);
        }

        public override bool IsHintAllowed(object hint)
        {
            return hint == null || hint as string == "";
        }

        public override IFocusable GetUpFocusableElement(Parent parent, Component previousFocus)
        {
            if (Orientation != Orientation.Vertical)
                return null;

            return FocusableBefore(parent, previousFocus);
        }

        public override IFocusable GetDownFocusableElement(Parent parent, Component previousFocus)
        {
            if (Orientation != Orientation.Vertical)
                return null;

            return FocusableAfter(parent, previousFocus);
        }

        public override IFocusable GetPreviousFocusableElement(Parent parent, Component previousFocus)
        {
            if (Orientation != Orientation.Horizontal)
                return null;

            return FocusableBefore(parent, previousFocus);
        }

        public override IFocusable GetNextFocusableElement(Parent parent, Component previousFocus)
        {
            if (Orientation != Orientation.Horizontal)
                return null;

            return FocusableAfter(parent, previousFocus);
        }

        private IFocusable FocusableAfter(Parent parent, Component previousFocus)
        {
            if (previousFocus == null)
                return (IFocusable) parent.GetChildren()
                    .First(i => i.Visible && ((i as IFocusable)?.IsFocusable() ?? false));

            for (var i = parent.GetChildren().IndexOf(previousFocus) + 1; i < parent.GetChildren().Count; i++)
            {
                var c = parent.GetChildren()[i];
                if (c.Visible && ((c as IFocusable)?.IsFocusable() ?? false)) return (IFocusable) c;
            }

            return null;
        }

        private IFocusable FocusableBefore(Parent parent, Component previousFocus)
        {
            if (previousFocus == null)
                return (IFocusable) parent.GetChildren()
                    .Last(i => i.Visible && ((i as IFocusable)?.IsFocusable() ?? false));

            for (var i = parent.GetChildren().IndexOf(previousFocus) - 1; i >= 0; i--)
            {
                var c = parent.GetChildren()[i];
                if (c.Visible && ((c as IFocusable)?.IsFocusable() ?? false)) return (IFocusable) c;
            }

            return null;
        }
    }
}
