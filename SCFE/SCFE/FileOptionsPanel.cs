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
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Viu;
using Viu.Components;
using Viu.Strategy;
using Viu.Table;

namespace SCFE
{
    public class FileOption
    {
        public static readonly Func<ImmutableList<File>, object, bool> OnlyIfSingleFile =
            (files, o) => files.Count == 1;

        public static readonly Func<ImmutableList<File>, object, bool> Always = (files, o) => true;

        public static readonly List<FileOption> OptionsForCurrentDirectory = new List<FileOption>();

        public static readonly List<FileOption> Options = new List<FileOption>();


        public string Title { get; set; }
        public string ActionName { get; set; }
        public Func<ImmutableList<File>, object, bool> CanActionBeApplied { get; set; }
    }

    public class FileOptionsPanel : Parent
    {
        public FileOptionsPanel(ImmutableList<File> f, AbstractHierarchicalDictionary<KeyStroke, string> inputMap)
        {
            TextComponent fileName;
            Strategy = new BorderStrategy();

            var topParent = new Parent(new LineStrategy {Orientation = Orientation.Vertical});
            AddComponent(topParent, BorderStrategy.Top);
            fileName = new TextComponent(f == null ? "Current directory options" :
                    f.Count == 1 ? f[0].GetFileName() : f.Count + " files...")
                {HAlign = HorizontalAlignment.Centered};
            topParent.AddComponent(fileName);
            topParent.AddComponent(new Separator());

            var table = new TableComponent<FileOption>
            {
                Data = new ObservableCollection<FileOption>(f == null
                    ? FileOption.OptionsForCurrentDirectory
                        .Where(opt => opt.CanActionBeApplied?.Invoke(null, null) ?? true).ToList()
                    : FileOption.Options
                        .Where(opt => opt.CanActionBeApplied(f, null)).ToList()),
                ShowHeader = false
            };
            table.AddColumn(new IndicatorColumnType<FileOption>());
            table.AddColumn(new BasicColumnType<FileOption>("", option => option.Title)
            {
                GrowPriority = 1
            });
            table.AddColumn(
                new BasicColumnType<FileOption>("", option => SearchForShortcut(option.ActionName, inputMap))
                {
                    HAlign = HorizontalAlignment.Right
                });
            table.ActionOnListElement += (sender, args) => { RemovalCallback(args.Item.ActionName, args.Graphics); };
            AddComponent(table, BorderStrategy.Center);

            var btn = new Button("Back");
            btn.ActionOnComponent += (sender, args) => RemovalCallback?.Invoke(null, args.Graphics);
            AddComponent(btn, BorderStrategy.Bottom);

            ActionMap.Put(StandardActionNames.CancelAction, (o, args) => RemovalCallback?.Invoke(null, args.Graphics));
        }

        public Action<string, GraphicsContext> RemovalCallback { get; set; }

        private string SearchForShortcut(string optionActionName, AbstractHierarchicalDictionary<KeyStroke, string> dic)
        {
            var mode = dic.Compile();
            foreach (var (k, v) in NavigationMode.NavMode.Bindings)
                if (mode.ContainsKey(k))
                    mode[k] = v;
                else
                    mode.Add(k, v);

            try
            {
                return "[" + mode.Where(pair => pair.Value == optionActionName).Select(pair => pair.Key)
                           .Select(key =>
                           {
                               var s = "";
                               if (key.Control == true)
                                   s += "Ctrl+";
                               if (key.Alt == true)
                                   s += "Alt+";
                               if (key.Shift == true)
                                   s += "Shift+";
                               if (key.Key != null)
                                   s += Enum.GetName(typeof(ConsoleKey), key.Key);
                               else if (key.KeyLetter != null)
                                   s += char.ToUpperInvariant(key.KeyLetter.Value);
                               return s;
                           }).Aggregate((cur, next) => cur + " | " + next) + "]";
            }
            catch (Exception)
            {
                // ignored
                return "";
            }
        }
    }
}
