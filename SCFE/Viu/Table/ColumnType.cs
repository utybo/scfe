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
using JetBrains.Annotations;
using Viu.Components;

namespace Viu.Table
{
    public abstract class ColumnType<T>
    {
        public int Priority { get; set; }

        public int GrowPriority { get; set; }

        public int ShrinkPriority { get; set; }

        [NotNull]
        public abstract Component GetRowInformation(T data, int index, int width, bool isFocused, bool isSelected,
            TableComponent<T> parent);

        public abstract int[] GetPossibleWidths(ICollection<T> data);

        public abstract int GetMaximumRowHeight(ICollection<T> data);

        public abstract string GetTitle(TableComponent<T> parent, int width);

        public abstract int GetTotalRowHeight(ObservableCollection<T> data);

        public virtual bool IsVisible(IEnumerable<T> enumerable)
        {
            return true;
        }
    }
}
