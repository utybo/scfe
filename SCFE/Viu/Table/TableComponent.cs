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
using System.Collections.Specialized;
using System.Linq;
using Viu.Components;

namespace Viu.Table
{
    public class TableComponent<T> : Container, IActionable
    {
        private int[] _columnWidths;
        private Component[,] _componentsGrid;
        private ObservableCollection<T> _data;
        private int _displayOffset;
        private bool _focused;
        private int _focusedIndex = -1;
        private int _headerSize = 1;

        private List<T> _selected = new List<T>();
        // Uses a container because we need a validation step to lay out the components of the table

        /// <summary>
        ///     The size of the horizontal gap to put between rows. 0 by default.
        /// </summary>
        public int RowGap { get; set; } = 0;

        /// <summary>
        ///     The size of the vertical gap to put between columns. 1 by default.
        /// </summary>
        public int ColumnGap { get; set; } = 1;

        public bool ShowHeader { get; set; } = true;

        private List<ColumnType<T>> Columns { get; } = new List<ColumnType<T>>();

        public ObservableCollection<T> Data
        {
            get => _data;
            set
            {
                if (_data == value)
                    return;

                if (_data != null)
                    _data.CollectionChanged -= WatchCollection;
                _data = value;
                _data.CollectionChanged += WatchCollection;
                _selected.Clear();
                _focusedIndex = -1;
            }
        }

        public T FocusedElement
        {
            get
            {
                if (_focusedIndex >= 0 && _focusedIndex < _data.Count)
                    return _data[_focusedIndex];
                return default;
            }

            set
            {
                var newIndex = Data.IndexOf(value);
                if (newIndex != -1) TryFocus(newIndex);
            }
        }

        public int FocusedIndex
        {
            get => _focusedIndex;
            set => TryFocus(value);
        }

        public ImmutableList<T> SelectedElements => _selected.ToImmutableList();
        public bool TrackTableChanges { get; set; } = true;
        public bool QuickClearOnNextPrint { get; set; }
        public event EventHandler<ActionEventArgs> ActionOnComponent;
        public event EventHandler<ListActionEventArgs<T>> ActionOnListElement;

        public void AddColumn(ColumnType<T> colInfo)
        {
            Columns.Add(colInfo);
        }

        private void WatchCollection(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (!TrackTableChanges)
                return;

            // The collection was changed
            GetEventThread().DoGraphicsAndWait(g => g.ClearArea(X, Y, Width, Height));

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _selected.Clear();
                    _focusedIndex = -1;
                    _displayOffset = 0;
                    break;
                case NotifyCollectionChangedAction.Remove when args.OldStartingIndex == _focusedIndex:
                {
                    foreach (var old in args.OldItems)
                        if (old is T t && _selected.Contains(t))
                            _selected.Remove(t);

                    if (TryFocus(_focusedIndex) == false && TryFocus(_focusedIndex - 1) == false)
                        FocusFirstElement();
                    break;
                }
                case NotifyCollectionChangedAction.Remove when args.OldStartingIndex < _focusedIndex:
                {
                    if (TryFocus(_focusedIndex - 1) == false)
                        FocusFirstElement();
                    break;
                }
            }

            // Just do a dirty validate+print for now

            GetEventThread().DoGraphicsAndWait(g =>
            {
                Validate();
                Print(g);
            });
        }


        /// <summary>
        ///     Try to focus the element at the given index. Returns true of successful or false if the index could not be
        ///     focused for whatever reason
        /// </summary>
        /// <param name="focusedIndex"></param>
        /// <returns>
        ///     False if focusing is impossible, true if it was possible and quick-reprint should be done, null if
        ///     it was successful and full reprint should be done. Cheap man's tri-state boolean!
        /// </returns>
        private bool? TryFocus(int focusedIndex)
        {
            if (focusedIndex < 0 || focusedIndex >= _data.Count)
                return false;
            _focusedIndex = focusedIndex;
            if (IndexVisible(_focusedIndex))
                return true;

            _displayOffset = focusedIndex - _displayOffset >= 0
                ? _focusedIndex - Height + _headerSize + 1
                : _focusedIndex;
            QuickClearOnNextPrint = true;
            return null;
        }

        private bool IndexVisible(int focusedIndex)
        {
            return focusedIndex - _displayOffset < Height - _headerSize && focusedIndex - _displayOffset >= 0;
        }

        private void FocusFirstElement()
        {
            _focusedIndex = -1;
            _displayOffset = 0;
            if (_data.Count > 0)
                _focusedIndex = 0;
        }

        public void RefreshColumnSizes()
        {
            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();

            _componentsGrid = new Component[columns.Count, Data.Count];

            // --Compute each column's width--

            // First, set all of them to the minimum
            _columnWidths = new int[columns.Count];
            var possibleWidths = new List<int>[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                possibleWidths[i] = new List<int>(columns[i].GetPossibleWidths(Data));
                possibleWidths[i].Sort();
                _columnWidths[i] = possibleWidths[i].Min();
            }

            ShrinkComponents();
            ExpandComponents(possibleWidths);
        }

        private void BuildComponentsGrid()
        {
            if (Data.Count == 0)
            {
                _componentsGrid = null;
                return;
            }

            RefreshColumnSizes();

            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            // Populate the grid
            _headerSize = ShowHeader ? 1 : 0; // TODO not only use 1
            for (var i = 0; i < columns.Count; i++)
            {
                var colx = _columnWidths.Take(i).Sum() + ColumnGap * i;
                if (_columnWidths[i] > 0)
                    for (var j = 0; j < Data.Count; j++)
                    {
                        var item = Data[j];
                        var c = columns[i].GetRowInformation(item, j, _columnWidths[i],
                            j == _focusedIndex && _focused,
                            _selected.Contains(item), this);
                        c.X = X + colx;
                        c.Width = _columnWidths[i];
                        c.Height = 1; // force 1 for now
                        c.Y = Y + _headerSize + j - _displayOffset;
                        _componentsGrid[i, j] = c;
                    }
            }
        }

        private void ShrinkComponents()
        {
            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            var spaceTaken = _columnWidths.Sum() + ColumnGap * (columns.Count - 1);
            if (spaceTaken > Width)
            {
                var spaceToShrink = spaceTaken - Width;
                var totalShrink = columns.Select(c => c.ShrinkPriority).Sum();
                if (totalShrink > 0)
                    for (var i = 0; i < columns.Count; i++)
                        _columnWidths[i] -=
                            (int) Math.Ceiling((float) spaceToShrink * columns[i].ShrinkPriority / totalShrink);
            }
        }

        private void ExpandComponents(List<int>[] possibleWidths)
        {
            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            var widthLeft = Width - (columns.Count - 1) * ColumnGap - _columnWidths.Sum();

            var prios = columns.Select(info => info.Priority).Distinct().ToList();
            prios.Sort();
            prios.Reverse();

            // Increase the size of components based on their own maximum values
            foreach (var prio in prios)
                for (var i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    if (prio == column.Priority)
                    {
                        // Increase the width by one step
                        var current = _columnWidths[i];

                        // ReSharper disable twice AccessToModifiedClosure
                        // The modified "closure" is not relevant here since we use the predicate right away
                        // never leaving the for loop.
                        bool CanExpand(int x)
                        {
                            return x > current && widthLeft - (x - _columnWidths[i]) >= 0;
                        }

                        if (!possibleWidths[i].Exists(CanExpand))
                            continue;
                        try
                        {
                            var nextWidth = possibleWidths[i]
                                .Last(CanExpand); // Expand as much as possible
                            var newWidthLeft = widthLeft - (nextWidth - _columnWidths[i]);
                            // Already maximum length
                            if (newWidthLeft >= 0)
                            {
                                _columnWidths[i] = nextWidth;
                                widthLeft = newWidthLeft;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Could not expand component (the Last call failed)
                        }
                    }
                }

            // Increase the size of growth-enabled components even more (GrowPriority > 0)
            var growprios = columns.Select(info => info.GrowPriority).ToList();
            double totalGrow = growprios.Sum();
            if (totalGrow > 0 && widthLeft > 0)
                for (var i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    if (column.GrowPriority > 0 && widthLeft > 0)
                        _columnWidths[i] += (int) Math.Round(widthLeft * (column.GrowPriority / totalGrow));
                }
        }

        public override void Validate()
        {
            if (Visible)
                BuildComponentsGrid();
        }

        public bool SetFocusedElement(T t, GraphicsContext g)
        {
            var oldIndex = _focusedIndex;
            var result = TryFocus(Data.IndexOf(t));
            if (result == true)
            {
                ValidateAndPrint(_focusedIndex, g);
                ValidateAndPrint(oldIndex, g);
                return true;
            }

            if (result == null)
            {
                Validate();
                Print(g);
                return true;
            }

            return false;
        }

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;
            if (QuickClearOnNextPrint)
            {
                QuickClearOnNextPrint = false;
                g.ClearArea(X, Y, Width, Height);
            }

            for (var j = _displayOffset; j < Math.Min(Height - _headerSize + _displayOffset, Data.Count); j++)
            {
                var useBlueColors = _selected.Contains(Data[j]);
                var restoredFg = g.CurrentForeground;
                if (useBlueColors)
                    g.CurrentForeground = ConsoleColor.Blue;

                if (_focusedIndex == j && _focused)
                    g.SwapColors();
                g.ClearArea(X, Y + j + _headerSize - _displayOffset, Width, 1);
                if (_focusedIndex == j && _focused)
                    g.SwapColors();
                if (useBlueColors)
                    g.CurrentForeground = restoredFg;
            }

            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();

            // Print the headers
            if (ShowHeader)
            {
                var n = 0;
                foreach (var column in columns)
                {
                    var colx = _columnWidths.Take(n).Sum() + ColumnGap * n;
                    g.Write(X + colx, Y, column.GetTitle(this, _columnWidths[n]));
                    n++;
                }
            }

            if (_componentsGrid == null)
                return;
            // Print the various components for now
            for (var i = 0; i < columns.Count; i++)
            for (var j = _displayOffset; j < Math.Min(Height - _headerSize + _displayOffset, Data.Count); j++)
            {
                var useBlueColors = _selected.Contains(Data[j]);
                var restoredFg = g.CurrentForeground;
                if (useBlueColors)
                    g.CurrentForeground = ConsoleColor.Blue;
                if (j == _focusedIndex && _focused)
                {
                    g.SwapColors();
                    _componentsGrid[i, j].Print(g);
                    g.SwapColors();
                }
                else
                {
                    var c = _componentsGrid[i, j];
                    c.Print(g);
                }

                if (useBlueColors)
                    g.CurrentForeground = restoredFg;
            }
        }

        public override Dimensions ComputeDimensions()
        {
            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            return new Dimensions(columns.Sum(c => c.GetPossibleWidths(Data).Min()) + ColumnGap * (columns.Count - 1),
                Math.Min(1,
                    columns.Select(c => c.GetTotalRowHeight(Data)).Max() + RowGap * (Data.Count - 1) + _headerSize));
        }

        public virtual bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            var inputs = InputMap.Compile();
            bool? result;
            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveDown) &&
                (result = TryFocus(_focusedIndex + 1)) != false)
            {
                if (result == true)
                {
                    ValidateAndPrint(_focusedIndex, g);
                    ValidateAndPrint(_focusedIndex - 1, g);
                }
                else
                {
                    Validate();
                    Print(g);
                }

                return true;
            }

            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveUp) &&
                (result = TryFocus(_focusedIndex - 1)) != false)
            {
                if (result == true)
                {
                    ValidateAndPrint(_focusedIndex, g);
                    ValidateAndPrint(_focusedIndex + 1, g);
                }
                else
                {
                    Validate();
                    Print(g);
                }

                return true;
            }

            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.SelectAction))
            {
                TrySelect(_focusedIndex);
                ValidateAndPrint(_focusedIndex, g);
                return true;
            }

            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.BaseAction))
            {
                ActionOnComponent?.Invoke(this, new ActionEventArgs(this, keyPressed, g));
                ActionOnListElement?.Invoke(this,
                    new ListActionEventArgs<T>(this, keyPressed, _data[_focusedIndex], g));
                return true;
            }

            return false;
        }

        public void ValidateAndPrint(int focusedIndex, GraphicsContext g)
        {
            if (_componentsGrid == null || !IndexVisible(focusedIndex))
                return;
            var j = focusedIndex;
            {
                var useBlueColors = _selected.Contains(Data[j]);
                var restoredFg = g.CurrentForeground;
                if (useBlueColors)
                    g.CurrentForeground = ConsoleColor.Blue;

                if (_focusedIndex == j && _focused)
                    g.SwapColors();
                g.ClearArea(X, Y + j + _headerSize - _displayOffset, Width, 1);
                if (_focusedIndex == j && _focused)
                    g.SwapColors();
                if (useBlueColors)
                    g.CurrentForeground = restoredFg;
            }

            var columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            if (columns.Count != _columnWidths.Length)
            {
                RefreshColumnSizes();
                columns = Columns.Where(c => c.IsVisible(Data)).ToImmutableList();
            }
            var headerSize = ShowHeader ? 1 : 0; // TODO
            for (var i = 0; i < columns.Count; i++)
            {
                var item = Data[j];
                var colx = _columnWidths.Take(i).Sum() + ColumnGap * i;
                var c = columns[i].GetRowInformation(item, j, _columnWidths[i],
                    j == _focusedIndex && _focused,
                    _selected.Contains(item), this);
                c.X = X + colx;
                c.Width = _columnWidths[i];
                c.Height = 1; // force 1 for now
                c.Y = Y + headerSize + j - _displayOffset;
                _componentsGrid[i, j] = c;
                var useBlueColors = _selected.Contains(Data[j]);
                var restoredFg = g.CurrentForeground;
                if (useBlueColors)
                    g.CurrentForeground = ConsoleColor.Blue;
                if (j == _focusedIndex && _focused)
                {
                    g.SwapColors();
                    _componentsGrid[i, j].Print(g);
                    g.SwapColors();
                }
                else
                {
                    c.Print(g);
                }

                if (useBlueColors)
                    g.CurrentForeground = restoredFg;
            }
        }


        public void TrySelect(int focusedIndex)
        {
            var obj = _data[focusedIndex];
            if (_selected.Contains(obj))
                _selected.Remove(obj);
            else
                _selected.Add(obj);
        }

        public bool IsFocusable()
        {
            return Data.Count > 0 && Columns.Any(c => c.IsVisible(Data));
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            _focused = focused;
            if (focused)
            {
                if (_focusedIndex < 0 || _focusedIndex > _data.Count) FocusFirstElement();

                ValidateAndPrint(_focusedIndex, g);
            }
            else
            {
                if (_focusedIndex >= 0 && _focusedIndex < _data.Count)
                    ValidateAndPrint(_focusedIndex, g);
            }
        }

        public bool IsFocused()
        {
            return _focused;
        }

        public void FocusIndex(int dest, GraphicsContext g)
        {
            var oldIndex = _focusedIndex;
            var b = TryFocus(dest);
            if (b == true)
            {
                ValidateAndPrint(oldIndex, g);
                ValidateAndPrint(dest, g);
            }
            else if (b == null)
            {
                Validate();
                Print(g);
            }
        }

        public void ResetSelection()
        {
            _selected.Clear();
        }
    }
}
