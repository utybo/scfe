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
using System.Collections.ObjectModel;
using System.Linq;
using Viu.Components;
using Viu.Strategy;
using Viu.Table;

namespace SCFE
{
    public struct Example
    {
        public string Name;
        public int Number;
        public ConsoleColor Color;
    }

    public class ColorColumnType : ColumnType<Example>
    {
        public override Component GetRowInformation(Example data, int index, int width, bool isFocused, bool isSelected,
            TableComponent<Example> parent)
        {
            return new TextComponent
            {
                Text = typeof(ConsoleColor).GetEnumName(data.Color),
                Foreground = isFocused ? ConsoleColor.Black : data.Color,
                Background = isFocused ? data.Color : (ConsoleColor?) null
            };
        }

        public override int[] GetPossibleWidths(ICollection<Example> data)
        {
            // ReSharper disable once PossibleNullReferenceException
            return new[] {Math.Max(data.Select(e => typeof(ConsoleColor).GetEnumName(e.Color).Length).Max(), 5)};
        }

        public override int GetMaximumRowHeight(ICollection<Example> data)
        {
            return 1;
        }

        public override string GetTitle(TableComponent<Example> parent, int width)
        {
            return "Color";
        }

        public override int GetTotalRowHeight(ObservableCollection<Example> data)
        {
            return data.Count;
        }
    }

    public static class ViuExampleTable
    {
        public static Parent LaunchExample()
        {
            var data = new ObservableCollection<Example>
            {
                new Example {Name = "Test", Number = 7, Color = ConsoleColor.Cyan},
                new Example {Name = "Woosh", Number = 18, Color = ConsoleColor.Red},
                new Example {Name = "Yeet", Number = 68, Color = ConsoleColor.Gray}
            };

            var par = new Parent(new BorderStrategy()) {ClearAreaBeforePrint = true};

            var table = new TableComponent<Example> {Data = data};
            table.ActionOnListElement += (o, args) => { data.Remove(args.Item); };
            table.AddColumn(new IndicatorColumnType<Example>());
            table.AddColumn(new BasicColumnType<Example>("Name", e => e.Name));
            table.AddColumn(new ColorColumnType());
            par.AddComponent(table, BorderStrategy.Center);

            return par;
        }
    }
}
