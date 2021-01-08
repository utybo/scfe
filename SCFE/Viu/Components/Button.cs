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
    public class Button : Component, IActionable
    {
        public Button(string s)
        {
            Text = s;
        }

        public string Text { get; set; }

        public ConsoleColor? Foreground { get; set; } = null;

        public bool Focusable { get; set; } = true;

        public bool Focused { get; private set; }

        public bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            var inputs = InputMap.Compile();
            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.BaseAction))
            {
                ActionOnComponent?.Invoke(this, new ActionEventArgs(this, keyPressed, g));

                return true;
            }

            return false;
        }

        public bool IsFocusable()
        {
            return Focusable;
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            if (Focused != focused)
            {
                Focused = focused;
                Print(g);
            }
        }

        public bool IsFocused()
        {
            return Focused;
        }

        public event EventHandler<ActionEventArgs> ActionOnComponent;

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;
            var s = "[ " + Text + " ]";
            if (Focused)
            {
                if (Foreground != null)
                    g.WriteRevert(X, Y, s, Foreground.Value);
                else
                    g.WriteRevert(X, Y, s);
            }
            else
            {
                g.Write(X, Y, s, Foreground, null);
            }
        }

        public override Dimensions ComputeDimensions()
        {
            return new Dimensions(Text.Length + 4, 1);
        }
    }
}
