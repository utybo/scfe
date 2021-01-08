/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.ObjectModel;
using Viu;
using Viu.Components;
using Viu.Strategy;
using Viu.Table;

namespace SCFE
{
    public class ViuDemoType
    {
        public ViuDemoType(string name, Parent par)
        {
            Name = name;
            Parent = par;
        }

        public string Name { get; }
        public Parent Parent { get; }
    }

    public class ViuDemo
    {
        private readonly ObservableCollection<ViuDemoType> _demos = new ObservableCollection<ViuDemoType>
        {
            new ViuDemoType("Simple Border Strategy Demo", Program.TestSimpleBorder()),
            new ViuDemoType("Complex Border Strategy Demo", Program.TestBorders()),
            new ViuDemoType("Text Field Demo", Program.TestTextArea()),
            new ViuDemoType("Button Test", Program.TestButtons()),
            new ViuDemoType("Table Test", Program.TestTable()),
            new ViuDemoType("Customized table test", ViuExampleTable.LaunchExample()),
            new ViuDemoType("Flow Test", Program.TestFlow())
        };

        public ViuDemo()
        {
            var mainContainer = new Parent {ClearAreaBeforePrint = true};
            var cons = new ConsoleParent(mainContainer);
            cons.SetTitle("Viu/SCFE Demo");
            var switcher = new SwitcherStrategy(mainContainer);
            mainContainer.Strategy = switcher;


            var menu = new Parent(new LineStrategy {Centered = true});
            mainContainer.AddComponent(menu);
            menu.AddComponent(new TextComponent("~ SCFE Demo ~")
                {Foreground = ConsoleColor.Yellow, HAlign = HorizontalAlignment.Centered});
            menu.AddComponent(new Separator());
            switcher.SwitchToComponent(menu, null);

            var table = new TableComponent<ViuDemoType> {Data = _demos};
            menu.AddComponent(table);
            table.AddColumn(new IndicatorColumnType<ViuDemoType>());
            table.AddColumn(
                new BasicColumnType<ViuDemoType>("Choose a demo to get started. Exit a demo with the Escape key.",
                    vdt => vdt.Name));
            table.ActionOnListElement += (sender, args) =>
            {
                switcher.SwitchToComponent(args.Item.Parent, args.Graphics);
                mainContainer.Validate();
                mainContainer.Print(args.Graphics);
            };

            foreach (var vdt in _demos)
                mainContainer.AddComponent(vdt.Parent);

            mainContainer.ActionMap.Put(StandardActionNames.CancelAction, (o, args) =>
            {
                mainContainer.SetFocused(false, args.Graphics);
                switcher.SwitchToComponent(menu, args.Graphics);
                mainContainer.Validate();
                mainContainer.SetFocused(true, args.Graphics);
                mainContainer.Print(args.Graphics);
            });

            cons.Validate();
            cons.FocusFirst();
            cons.Print();
        }
    }
}
