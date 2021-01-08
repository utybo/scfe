/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using Viu;
using Viu.Table;

namespace SCFE
{
    public class ScfeTable : TableComponent<File>
    {
        private readonly ScfeApp _app;
        private bool _escPressedOnce;

        public ScfeTable(ScfeApp app)
        {
            _app = app;
        }

        public override bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            if (_escPressedOnce)
            {
                if (keyPressed.Key == ConsoleKey.Escape)
                {
                    if (_app.Tasks.GetTasks().Count != 0)
                    {
                        _app.ShowHelpMessage("Tasks are still running, try again later", g, ConsoleColor.Red);
                        _escPressedOnce = false;
                        return true;
                    }

                    g.Clear();
                    Environment.Exit(0);
                    return true;
                }

                _app.ShowHelpMessage("Exit cancelled (press Escape twice to exit)", g);
                _escPressedOnce = false;
            }

            var b = base.AcceptInput(keyPressed, g);
            if (b) return true;
            
            if (keyPressed.Key == ConsoleKey.Escape)
            {
                _app.ShowHelpMessage("Press Escape again to exit SCFE", g, ConsoleColor.Yellow);
                _escPressedOnce = true;
                return true;
            }

            if (_app.CurrentMode.SearchEnabled && keyPressed.KeyChar != (char) 0 &&
                !char.IsControl(keyPressed.KeyChar) &&
                keyPressed.Key != ConsoleKey.Enter &&
                (keyPressed.Modifiers & ConsoleModifiers.Control) == 0 &&
                (keyPressed.Modifiers & ConsoleModifiers.Alt) == 0)
            {
                SetFocused(false, g);
                _app.TextBox.Text += keyPressed.KeyChar;
                _app.TextBox.CaretPosition += 1;
                _app.TextBox.Print(_app.TextBox.Width - 5, g);

                if (_app.TextBoxHandler == null)
                    _app.SwitchToFolder(_app.CurrentDir, g);
                _app.TextBox.SetFocused(true, g);
                return true;
            }

            return false;
        }
    }
}
