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
    public class TextField : Component, ICursorFocusable, IActionable
    {
        private bool Focused { get; set; }

        public bool Focusable { get; set; } = true;
        public string Text { get; set; } = "";

        public bool HideText { get; set; }
        public string PlaceholderText { get; set; } = "";

        public int CaretPosition { get; set; }

        public bool ShowLine { get; set; } = true;

        public string DisplayedText => HideText
            ? "(hidden for privacy)"
            : Text.Length > 0
                ? Text
                : PlaceholderText;

        public event EventHandler<ActionEventArgs> ActionOnComponent;

        public bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            g.SetCursorVisible(false);
            var map = InputMap.Compile();

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveLeftWord))
            {
                // DOING
                // Move one word to the left, i.e. move to the left until you meet a space (or the beginning of the line)

                var i = CaretPosition - 1;
                var skipSpaces = true;
                // this is so that we jump to the previous word if we are at the beginning of a
                // word when ctrl <- is invoked

                while (CaretPosition >= 0 && CaretPosition <= Text.Length)
                {
                    if (CaretPosition == 0)
                    {
                        UpdateCursorState(g);
                        return true;
                    }

                    switch (Text[i])
                    {
                        case ' ':
                            if (!skipSpaces)
                            {
                                UpdateCursorState(g);
                                return true;
                            }

                            goto default; // Jump to case default
                        default:
                            CaretPosition--;
                            i--;
                            skipSpaces = false;
                            break;
                    }
                }

                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveRightWord))
            {
                var skipSpaces = true;

                while (CaretPosition <= Text.Length)
                {
                    if (CaretPosition == Text.Length)
                    {
                        UpdateCursorState(g);
                        return true;
                    }

                    switch (Text[CaretPosition])
                    {
                        case ' ':
                            if (!skipSpaces)
                            {
                                UpdateCursorState(g);
                                return true;
                            }

                            CaretPosition++;
                            break;
                        default:
                            CaretPosition++;
                            skipSpaces = false;
                            break;
                    }
                }

                UpdateCursorState(g);
                return false;
            }


            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.BaseAction))
            {
                ActionOnComponent?.Invoke(this, new ActionEventArgs(this, keyPressed, g));
                UpdateCursorState(g);
                return true;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveLeft))
            {
                if (CaretPosition > 0)
                {
                    CaretPosition -= 1;
                    UpdateCursorState(g);
                    return true;
                }

                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveRight))
            {
                if (CaretPosition < Text.Length)
                {
                    CaretPosition += 1;
                    UpdateCursorState(g);
                    return true;
                }

                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.DeleteToTheLeftAction))
            {
                if (CaretPosition > 0)
                {
                    Text = Text.Remove(CaretPosition - 1, 1);
                    CaretPosition -= 1;
                    Print(1, g);
                    OnTextChanged?.Invoke(this, new ActionEventArgs(this, keyPressed, g));

                    UpdateCursorState(g);
                    return true;
                }

                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.DeleteToTheRightAction))
            {
                if (CaretPosition > 0 && CaretPosition < Text.Length)
                {
                    Text = Text.Remove(CaretPosition, 1);
                    Print(1, g);
                    OnTextChanged?.Invoke(this, new ActionEventArgs(this, keyPressed, g));

                    UpdateCursorState(g);
                    return true;
                }


                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveLineStart))
            {
                // TODO
                // Move the caret to the beginning of the line
                if (CaretPosition > 0)
                {
                    CaretPosition = 0;

                    UpdateCursorState(g);
                    return true;
                }

                UpdateCursorState(g);
                return false;
            }

            if (Utils.KeyCorresponds(map, keyPressed, StandardActionNames.MoveLineEnd))
            {
                // TODO
                // Move the caret to the end of the line
                if (CaretPosition < Text.Length)
                {
                    CaretPosition = Text.Length;
                    UpdateCursorState(g);
                    return true;
                }

                UpdateCursorState(g);
                return false;
            }

            // Letters: add the letters to Text and write them with the following line. Also increase CaretPosition.
            //     g.Write(X + CaretPosition, Y, <the letter>);
            // Backspace: delete the letter, careposition - 1, make sure to rewrite the string correctly (or call Print
            // if you're lazy)

            if (keyPressed.KeyChar > (char) 31 && keyPressed.Key != ConsoleKey.Enter &&
                keyPressed.Key != ConsoleKey.Tab && keyPressed.Key != ConsoleKey.Escape &&
                // NOT(control pressed XOR alt pressed)
                (keyPressed.Modifiers & ConsoleModifiers.Control) == 0 ==
                ((keyPressed.Modifiers & ConsoleModifiers.Alt) == 0))
            {
                Text = Text.Insert(CaretPosition, "" + keyPressed.KeyChar);
                CaretPosition += 1;
                Print(PlaceholderText.Length - 1, g);
                OnTextChanged?.Invoke(this, new ActionEventArgs(this, keyPressed, g));
                UpdateCursorState(g);
                return true;
            }


            // USE .KeyChar AND NOT .Key.ToString() !!!

            // If it matched one of the cases above, return true
            // Anything else: return false
            UpdateCursorState(g);
            return false;
        }

        public bool IsFocusable()
        {
            return Focusable;
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            Focused = focused;
            UpdateCursorState(g);
        }

        public bool IsFocused()
        {
            return Focused;
        }

        public void UpdateCursorState(GraphicsContext g)
        {
            if (Focused)
            {
                if (HideText)
                    g.SetCursorPosition(X, Y);
                else
                    g.SetCursorPosition(X + CaretPosition, Y);
                g.SetCursorVisible(true);
            }
            else
            {
                g.SetCursorVisible(false);
            }
        }

        public event EventHandler<ActionEventArgs> OnTextChanged;

        public override void Print(GraphicsContext g)
        {
            Print(0, g);
        }

        public void Print(int withPaddingForced, GraphicsContext g)
        {
            if (!Visible)
                return;

            var (toPrint, color) =
                HideText
                    ? ("(hidden for privacy)", ConsoleColor.Cyan)
                    : Text.Length > 0
                        ? (Text, (ConsoleColor?) null)
                        : (PlaceholderText, ConsoleColor.DarkGray);

            if (toPrint.Length > 0)
                g.Write(X, Y,
                    toPrint.Length > Width
                        ? toPrint.Substring(0, Width)
                        : withPaddingForced > 0
                            ? toPrint + new string(' ',
                                  Math.Min(Width - toPrint.Length, withPaddingForced))
                            : toPrint,
                    color, null);

            if (ShowLine)
                g.Write(X, Y + 1, new string(LineStyle.Dotted.Horizontal, Width));

            g.SetCursorPosition(X + CaretPosition, Y);
        }

        public override Dimensions ComputeDimensions()
        {
            return new Dimensions(Math.Max(20, HideText ? 20 : Text == "" ? PlaceholderText.Length : Text.Length),
                ShowLine ? 2 : 1);
        }
    }
}
