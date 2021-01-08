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

namespace Viu.Components
{
    /// <summary>
    ///     A TextComponent displays a single line of text which can have a specific foreground color.
    ///     A TextComponent can be focused by setting the Focusable property to true. A focused TextComponent does not "do"
    ///     anything, but its behavior can be customized by overriding AcceptInput.
    /// </summary>
    public class TextComponent : Component, IFocusable
    {
        public TextComponent()
        {
        }

        public TextComponent(string s)
        {
            Text = s;
        }

        /// <summary>
        ///     The text to display
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        ///     The foreground color to use when printing the text. Can be set to null to use the default one.
        /// </summary>
        public ConsoleColor? Foreground { get; set; }

        public ConsoleColor? Background { get; set; }

        /// <summary>
        ///     If true, the Text Component will be focusable.
        /// </summary>
        public bool Focusable { get; set; }

        /// <summary>
        ///     True when the text component currently has the focus
        /// </summary>
        private bool HasFocus { get; set; }

        public bool ReverseColors { get; set; }

        public HorizontalAlignment HAlign { get; set; } = HorizontalAlignment.Left;

        public VerticalAlignment VAlign { get; set; } = VerticalAlignment.Top;

        public HorizontalAlignment CutOverflowFrom { get; set; } = HorizontalAlignment.Right;

        public bool ClearBlankSpaceOnReprint { get; set; }

        public virtual bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            // Do nothing, nothing is consumed
            return false;
        }

        public bool IsFocusable()
        {
            return Visible && Focusable;
        }

        public void SetFocused(bool b, GraphicsContext g)
        {
            HasFocus = b;
            Print(g);
        }

        public bool IsFocused()
        {
            return HasFocus;
        }

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;
            var disp = Text;
            if (disp.Length > Width)
                switch (CutOverflowFrom)
                {
                    case HorizontalAlignment.Right:
                        disp = disp.Substring(0, Width);
                        if (disp.Length > 0)
                        {
                            var sb = new StringBuilder(disp);
                            sb[disp.Length - 1] = '…';
                            disp = sb.ToString();
                        }

                        break;
                    case HorizontalAlignment.Centered:
                        var str1 = disp.Substring(0, Width % 2 == 0 ? Width / 2 - 1 : Width / 2);
                        var str2 = disp.Substring(disp.Length - Width / 2, Width / 2);
                        disp = disp.Length > 0 ? str1 + '…' + str2 : "";
                        break;
                    case HorizontalAlignment.Left:
                        disp = disp.Substring(disp.Length - Width, Width);
                        if (disp.Length > 0)
                        {
                            var sb = new StringBuilder(disp);
                            sb[0] = '…';
                            disp = sb.ToString();
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            if (ClearBlankSpaceOnReprint && disp.Length < Width) 
                disp += new string(' ', Width - disp.Length);
            int startX, startY;

            switch (VAlign)
            {
                case VerticalAlignment.Top:
                    startY = Y;
                    break;
                case VerticalAlignment.Bottom:
                    startY = Y + Height - 1;
                    break;
                case VerticalAlignment.Centered:
                    startY = Y + Height / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (HAlign)
            {
                case HorizontalAlignment.Left:
                    startX = X;
                    break;
                case HorizontalAlignment.Right:
                    startX = X + Width - disp.Length;
                    break;
                case HorizontalAlignment.Centered:
                    startX = X + (Width - disp.Length) / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!HasFocus && !ReverseColors || HasFocus && ReverseColors)
            {
                if (Foreground == null && Background == null)
                    g.Write(startX, startY, disp);
                else
                    g.Write(startX, startY, disp, Foreground, Background);
            }
            else
            {
                // TODO support background
                if (Foreground == null)
                    g.WriteRevert(startX, startY, disp);
                else
                    g.WriteRevert(startX, startY, disp, Foreground.Value);
            }
        }


        public override Dimensions ComputeDimensions()
        {
            return new Dimensions(Text.Length, 1);
        }
    }
}
