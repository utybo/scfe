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
using JetBrains.Annotations;
using MoreLinq;
using Viu.Components;

namespace Viu.Table
{
    public class MultistateColumnType<T> : ColumnType<T>
    {
        private readonly Func<T, ConsoleColor?> _colorGetter;
        private readonly List<string> _titles;
        private readonly Func<T, IEnumerable<string>> _translators;
        [CanBeNull] private readonly Func<IEnumerable<T>, bool> _visibility;

        public MultistateColumnType(IEnumerable<string> titles, Func<T, IEnumerable<string>> toRowString,
            Func<IEnumerable<T>, bool> visibilityGetter = null, Func<T, ConsoleColor?> colorGetter = null)
        {
            _titles = new List<string>(titles);
            _titles.Sort((x, y) => x.Length.CompareTo(y.Length));
            _translators = toRowString;
            _visibility = visibilityGetter;
            _colorGetter = colorGetter;
        }

        public HorizontalAlignment HAlign { get; set; } = HorizontalAlignment.Left;

        public override Component GetRowInformation(T data, int index, int width, bool isFocused, bool isSelected,
            TableComponent<T> parent)
        {
            if (data == null)
                return new TextComponent();
            var possibilities = new List<string>(_translators(data));
            possibilities.Sort((s1, s2) => s1.Length.CompareTo(s2.Length));
            return possibilities.Any(s => s.Length <= width)
                ? new TextComponent(possibilities.Last(s => s.Length <= width))
                {
                    HAlign = HAlign,
                    Background = isFocused ? _colorGetter?.Invoke(data) : null,
                    Foreground = isFocused ? null : _colorGetter?.Invoke(data)
                }
                : new TextComponent(possibilities.MinBy(s => s.Length).First())
                {
                    Background = isFocused ? _colorGetter?.Invoke(data) : null,
                    Foreground = isFocused ? null : _colorGetter?.Invoke(data)
                };
        }

        public override int[] GetPossibleWidths(ICollection<T> data)
        {
            var methodsMaxSize = new List<int>();

            foreach (var t in data)
            {
                var lengths = _translators(t).Select(s => { return s.Length; }).ToImmutableList();
                for (var i = 0; i < lengths.Count; i++)
                    if (i >= methodsMaxSize.Count)
                        methodsMaxSize.Add(lengths[i]);
                    else
                        methodsMaxSize[i] = Math.Max(lengths[i], methodsMaxSize[i]);
            }

            var minTitle = _titles.Select(s => s.Length).Min();
            var l = methodsMaxSize.Select(x => Math.Max(x, minTitle)).Distinct().ToList();
            // If the title might be bigger, also add that as a possible width
            if (l.Max() < _titles.Last().Length)
                l.Add(_titles.Last().Length);
            l.Sort();
            return l.ToArray();
        }

        public override int GetMaximumRowHeight(ICollection<T> data)
        {
            return 1;
        }

        public override string GetTitle(TableComponent<T> parent, int width)
        {
            return _titles.Last(s => s.Length <= width);
        }

        public override int GetTotalRowHeight(ObservableCollection<T> data)
        {
            return data.Count;
        }

        public override bool IsVisible(IEnumerable<T> data)
        {
            return _visibility?.Invoke(data) ?? true;
        }
    }
}
