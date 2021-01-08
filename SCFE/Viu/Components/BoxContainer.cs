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
    /// <inheritdoc cref="Container" />
    /// <inheritdoc cref="IFocusable" />
    /// <summary>
    ///     A BoxContainer only contains one element which is wrapped inside a box. The style of said box can be changed by
    ///     changing the Style property.
    /// </summary>
    public class BoxContainer : Container, ICursorFocusable
    {
        /// <summary>
        ///     Create a new BoxContainer with the given component
        /// </summary>
        /// <param name="c">The component contained by this BoxContainer</param>
        public BoxContainer(Component c)
        {
            SetComponent(c);
        }

        /// <summary>
        ///     Create a new BoxContainer with the given component and with the given LineStyle
        /// </summary>
        /// <param name="c">The component contained by this BoxContainer</param>
        /// <param name="style"></param>
        public BoxContainer(Component c, LineStyle style)
        {
            SetComponent(c);
            Style = style;
        }

        /// <summary>
        ///     The LineStyle used to print the borders of this BoxContainer
        /// </summary>
        public LineStyle Style { get; set; } = LineStyle.Simple;

        public bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            var ins = Components[0].InputMap.Compile();
            var str = Utils.GetActionNameForKey(ins, keyPressed);
            var b = (Components[0] as IFocusable)?.AcceptInput(keyPressed, g) ?? false;

            if (b)
                return true;

            if (str != null)
            {
                var action = Components[0].ActionMap.Get(str);
                if (action != null)
                {
                    action(Components[0], new ActionEventArgs(Components[0], keyPressed, g));
                    return true;
                }
            }

            return false;
        }

        public bool IsFocusable()
        {
            return (Components[0] as IFocusable)?.IsFocusable() ?? false;
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            (Components[0] as IFocusable)?.SetFocused(focused, g);
        }

        public bool IsFocused()
        {
            return (Components[0] as IFocusable)?.IsFocused() ?? false;
        }

        public void UpdateCursorState(GraphicsContext g)
        {
            (Components[0] as ICursorFocusable)?.UpdateCursorState(g);
        }

        /// <summary>
        ///     Set this BoxContainer's component to the given one. This method does NOT redraw the BoxContainer.
        /// </summary>
        /// <param name="c">The component to use in this BoxContainer</param>
        public void SetComponent(Component c)
        {
            // todo handle c == null
            Components.Clear();
            Components.Add(c);
            c.Parent = this;
        }

        public override Dimensions ComputeDimensions()
        {
            if (Components.Count > 0)
            {
                var d = Components[0].ComputeDimensions();
                return d.Add(2, 2);
            }

            return new Dimensions(2, 2);
        }

        public override void Validate()
        {
            if (Components.Count > 0)
            {
                var c = Components[0];
                c.X = X + 1;
                c.Y = Y + 1;
                c.Height = Height - 2;
                c.Width = Width - 2;
                if (c is Container ct)
                    ct.Validate();
            }
        }

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;

            g.Write(X, Y, Style.TopLeft + new string(Style.Horizontal, Width - 2) + Style.TopRight);
            for (var i = 1; i < Height - 1; i++)
                g.Write(X, Y + i, Style.Vertical + new string(' ', Width - 2) + Style.Vertical);

            g.Write(X, Y + Height - 1, Style.BottomLeft + new string(Style.Horizontal, Width - 2) + Style.BottomRight);

            if (Components.Count > 0)
            {
                Components[0].Print(g);
                (Components[0] as ICursorFocusable)?.UpdateCursorState(g);
            }
        }
    }
}
