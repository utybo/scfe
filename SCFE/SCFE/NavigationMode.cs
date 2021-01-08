/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Viu;

namespace SCFE
{
    public class NavigationMode
    {
        public static readonly NavigationMode NavMode = new NavigationMode("NAV", null, new Dictionary<KeyStroke, string>
        {
            {new KeyStroke(ConsoleKey.Delete, null, null, null), ScfeActions.DeleteFile},
            {new KeyStroke(ConsoleKey.Spacebar, false, false, false), StandardActionNames.SelectAction},
            {new KeyStroke(ConsoleKey.Enter, true, false, false), ScfeActions.ComMode},
            {new KeyStroke('m', null, false, false), ScfeActions.ChangeMode},
            {new KeyStroke('m', null, false, true), ScfeActions.ComMode},

            {new KeyStroke('n', null, false, false), ScfeActions.CreateFile},
            {new KeyStroke('n', null, false, true), ScfeActions.CreateFolder},
            {new KeyStroke('e', null, false, false), ScfeActions.ChangeMode},
            {new KeyStroke('d', null, false, false), ScfeActions.DeleteFile},
            {new KeyStroke('c', null, false, false), ScfeActions.CopyFile},
            {new KeyStroke('c', null, false, true), ScfeActions.GitCommit},
            {new KeyStroke('v', null, false, false), ScfeActions.PasteFile},
            {new KeyStroke('x', null, false, false), ScfeActions.CutFile},
            {new KeyStroke('g', null, false, false), ScfeActions.GoToFolder},
            {new KeyStroke('g', null, false, true), ScfeActions.GitClone},
            {new KeyStroke('s', null, false, false), StandardActionNames.SelectAction},
            {new KeyStroke('s', null, false, true), ScfeActions.ChangeSort},
            {new KeyStroke('q', null, false, true), ScfeActions.ToggleSortOrder},
            {new KeyStroke('r', null, false, false), ScfeActions.Rename},
            {new KeyStroke('r', null, false, true), ScfeActions.Refresh},
            {new KeyStroke('o', null, false, true), ScfeActions.CurrDirOptions},
            {new KeyStroke('o', null, false, false), StandardActionNames.SecondaryAction},
            {new KeyStroke('t', null, false, false), ScfeActions.ToggleShowHiddenFiles},
            {new KeyStroke('a', null, false, false), ScfeActions.SelectAll},
            {new KeyStroke('a', null, false, true), ScfeActions.ToggleSelection},
            {new KeyStroke('i', null, false, false), ScfeActions.GitStage},
            {new KeyStroke('i', null, false, true), ScfeActions.GitUnstage},
            {new KeyStroke('i', null, true, false), ScfeActions.GitInit},
            {new KeyStroke('p', null, false, false), ScfeActions.GitPush},
            {new KeyStroke('p', null, false, true), ScfeActions.GitPull},

            {new KeyStroke('j', false, false, false), StandardActionNames.MoveDown},
            {new KeyStroke('j', false, false, true), ScfeActions.GoDownFast},
            {new KeyStroke('k', false, false, false), StandardActionNames.MoveUp},
            {new KeyStroke('k', false, false, true), ScfeActions.GoUpFast},
            {new KeyStroke('h', false, false, false), StandardActionNames.MoveLeft},
            {new KeyStroke('l', false, false, false), StandardActionNames.MoveRight},
            {new KeyStroke('l', false, false, true), StandardActionNames.SecondaryAction}
        });

        public static readonly NavigationMode SearchMode;

        public static readonly NavigationMode ComMode = new NavigationMode("COM", ConsoleColor.DarkCyan, null);

        public static readonly List<NavigationMode> NavigationModes;

        static NavigationMode()
        {
            var dic = new Dictionary<KeyStroke, string>
            {
                {new KeyStroke(ConsoleKey.Spacebar, false, false, false), StandardActionNames.SelectAction}
            };
            foreach (var (k, v) in NavMode.Bindings)
                if (k.Control == null && k.Shift != null && k.Alt != null)
                {
                    if (k.Key != null)
                    {
                        dic.Add(new KeyStroke(k.Key.Value, true, k.Shift, k.Alt), v);
                    }
                    else
                    {
                        Debug.Assert(k.KeyLetter != null, "k.KeyLetter != null");
                        dic.Add(new KeyStroke(k.KeyLetter.Value, true, k.Shift, k.Alt), v);
                    }
                }
                else if (k.Control == true)
                {
                    dic.Add(k, v);
                }

            SearchMode = new NavigationMode("SEA", ConsoleColor.Magenta, dic) {SearchEnabled = true};
            NavigationModes = new List<NavigationMode> {NavMode, SearchMode};
        }

        public NavigationMode(string name, ConsoleColor? modeColor, Dictionary<KeyStroke, string> bindings)
        {
            Color = modeColor;
            Bindings = bindings;
            Name = name;
        }
        
        public ConsoleColor? Color { get; }

        public Dictionary<KeyStroke, string> Bindings { get; }
        public string Name { get; }

        public bool SearchEnabled { set; get; }
    }
}
