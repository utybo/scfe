/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Viu.Components;

namespace Viu.Table
{
    public class IndicatorColumnType<T> : ColumnType<T>
    {
        public override Component GetRowInformation(T data, int index, int width, bool isFocused, bool isSelected,
            TableComponent<T> parent)
        {
            if (isFocused && isSelected)
                return new TextComponent("=");
            if (isFocused)
                return new TextComponent(">");
            if (isSelected)
                return new TextComponent("*");
            return new TextComponent(" ");
        }

        public override int[] GetPossibleWidths(ICollection<T> data)
        {
            return new[] {1};
        }

        public override int GetMaximumRowHeight(ICollection<T> data)
        {
            return 1;
        }

        public override string GetTitle(TableComponent<T> parent, int width)
        {
            return " ";
        }

        public override int GetTotalRowHeight(ObservableCollection<T> data)
        {
            return data.Count;
        }
    }
}
