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
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Viu;
using Viu.Components;
using Viu.Strategy;
using Viu.Table;

namespace SCFE
{
    public class ScfeApp
    {
        private readonly TextComponent _elementsCount;
        private readonly TextComponent _filePath;

        private readonly ObservableCollection<File> _files = new ObservableCollection<File>();

        public List<FileComparer> FileSorters { get; } = new List<FileComparer>
        {
            new FileComparer("by name",
                "A -> Z",
                new CompositeComparer<File>(new TypeComparer(), new NameNaturalComparer()),
                "Z -> A",
                new CompositeComparer<File>(new TypeComparer(), new NameNaturalComparer().Reversed())),
            new FileComparer("by extension",
                ".A -> .Z",
                new CompositeComparer<File>(new TypeComparer(), new ExtensionComparer(), new NameNaturalComparer()),
                ".Z -> .A",
                new CompositeComparer<File>(new TypeComparer(), new ExtensionComparer().Reversed(),
                    new NameNaturalComparer().Reversed())),
            new FileComparer("by size",
                "big -> small",
                new CompositeComparer<File>(new TypeComparer(),
                    new CompositeComparer<File>(new SizeComparer().Reversed(), new NameNaturalComparer())),
                "small -> big",
                new CompositeComparer<File>(
                    new TypeComparer(),
                    new CompositeComparer<File>(new SizeComparer(), new NameNaturalComparer()))),
            new FileComparer("by date",
                "recent -> old",
                new CompositeComparer<File>(new TypeComparer(),
                    new CompositeComparer<File>(new DateComparer().Reversed(), new NameNaturalComparer())),
                "old -> recent",
                new CompositeComparer<File>(new TypeComparer(),
                    new CompositeComparer<File>(new DateComparer(), new NameNaturalComparer())))
        };

        public bool UseReverseSorting { get; set; }

        public FileComparer FileSorting { get; set; }


        private readonly IFilter<File> _filter, _noSearchFilter;
        private readonly TextComponent _help;
        private readonly Parent _main;
        private readonly TextComponent _mode;
        private readonly TableComponent<File> _table;
        private readonly SwappableAddinHierDic<KeyStroke, string> _tableInputMap;
        private readonly Parent _wrapper;
        internal readonly TaskPool<string> Tasks;
        public FileColorScheme ColorScheme { get; }
        private ConsoleParent _cons;
        private bool _filterOnBoxInput;
        public bool ShowHiddenFiles { get; set; }
        public ImmutableList<File> SelectedElements => _table.SelectedElements;
        public File FocusedElement => _table.FocusedElement;

        // TODO replace with File.CurrentDirectory when that will be done
        internal File CurrentDir;

        public NavigationMode CurrentMode { get; private set; }

        internal TextField TextBox;

        internal Action<object, ActionEventArgs> TextBoxHandler;

        private Action<GraphicsContext> _textBoxCancelHandler;

        private List<IScfeExtension> _extensions = new List<IScfeExtension>();

        public ImmutableList<File> ClipboardContent { get; set; }
        public FileTransferMode ClipboardMode { get; set; }

        public event EventHandler OnPreRefresh;

        public event EventHandler<FolderViewChangedEventArgs> OnFolderViewChanged;

        public event EventHandler<FileChangedEventArgs> OnFileChanged;

        public ScfeApp(File startingFile, string[] columnsToAdd)
        {
            CurrentDir = startingFile ?? new File(Directory.GetCurrentDirectory());
            _filter = new AndFilter<File>(
                new BoolFilter<File>(f => ShowHiddenFiles || !f.IsHidden()),
                new BranchingFilter<File>(() => _filterOnBoxInput && TextBoxHandler == null,
                    new StringFilter<File>(file => file.GetFileName().ToLowerInvariant(),
                        () => TextBox.Text.ToLowerInvariant()),
                    new TrueFilter<File>()));
            FileSorting = FileSorters[0];

            _noSearchFilter = new BoolFilter<File>(f => ShowHiddenFiles || !f.IsHidden());

            Tasks = new TaskPool<string>((task, tresult) =>
            {
                _cons?.DoGraphicsLater(g =>
                {
                    if (tresult.Message != null)
                        ShowHelpMessage(tresult.Message, g, tresult.Success ? ConsoleColor.Green : ConsoleColor.Red);
                });
            });

            _main = new Parent(new BorderStrategy());
            AddMainBindings();

            var p = new Parent(new LineStrategy());
            _main.AddComponent(p, BorderStrategy.Top);

            var p2 = new Parent(new BorderStrategy());
            p.AddComponent(p2);

            p2.AddComponent(new TextComponent("dir: ") {Foreground = ConsoleColor.DarkGray}, BorderStrategy.Left);

            _filePath = new TextComponent(CurrentDir.Path)
                {CutOverflowFrom = HorizontalAlignment.Centered, ClearBlankSpaceOnReprint = true};
            p2.AddComponent(_filePath, BorderStrategy.Center);

            _elementsCount = new TextComponent("[...]") {ClearBlankSpaceOnReprint = true};
            p2.AddComponent(_elementsCount, BorderStrategy.Right);

            p.AddComponent(new Separator {Foreground = ConsoleColor.DarkGray});

            p = new Parent(new LineStrategy());
            _main.AddComponent(p, BorderStrategy.Bottom);

            p.AddComponent(new Separator {Foreground = ConsoleColor.DarkGray});

            p2 = new Parent(new BorderStrategy());
            p.AddComponent(p2);

            var p3 = new Parent(new FlowStrategy(1));
            p2.AddComponent(p3, BorderStrategy.Left);

            _mode = new TextComponent("xxx");
            p3.AddComponent(_mode);

            p3.AddComponent(new Separator {Orientation = Orientation.Vertical, Foreground = ConsoleColor.DarkGray});
            p3.AddComponent(new TextComponent("")); // To have an extra separator at the end

            _help = new TextComponent("Welcome to SCFE!") {ClearBlankSpaceOnReprint = true};
            p2.AddComponent(_help, BorderStrategy.Center);

            p2 = new Parent(new BorderStrategy());
            p.AddComponent(p2);
            p2.AddComponent(new TextComponent("~> "), BorderStrategy.Left);

            TextBox = new TextField
            {
                PlaceholderText =
                    "Press E or M (in NAV mode, with Ctrl in SEA mode) to switch modes. Ctrl+Enter or (Ctrl+)Shift+M for COM mode.",
                ShowLine = false
            };
            TextBox.OnTextChanged += (sender, args) =>
            {
                if (_filterOnBoxInput && TextBoxHandler == null) SwitchToFolder(CurrentDir, args.Graphics);
            };
            TextBox.ActionOnComponent += (sender, args) =>
            {
                if (TextBoxHandler != null)
                {
                    TextBoxHandler(sender, args);
                }
                else if (_filterOnBoxInput)
                {
                    if (_table.Data.Count == 1 && _table.Data[0] != null)
                        HandlerOpenFileInTable(_table.Data[0], args.Graphics);

                    TextBox.SetFocused(false, args.Graphics);
                    _table.SetFocused(true, args.Graphics);
                }
            };
            TextBox.ActionMap.Put(StandardActionNames.CancelAction, (o, args) =>
            {
                args.Graphics.SetCursorVisible(false);
                if (TextBoxHandler != null)
                {
                    CancelTextBoxHandler(args.Graphics);
                }
                else if (_filterOnBoxInput)
                {
                    ResetTextBox(args.Graphics);
                    SwitchToFolder(CurrentDir, args.Graphics);
                    ResetFocusableStates(args.Graphics);
                }
            });
            p2.AddComponent(TextBox, BorderStrategy.Center);

            _wrapper = new Parent {ClearAreaBeforePrint = true};
            _wrapper.Strategy = new SwitcherStrategy(_wrapper);
            _main.AddComponent(_wrapper, BorderStrategy.Center);

            _extensions.Add(new BaseExtension(this));
            _extensions.Add(new ComExtension(this));

            var gitExt = new GitExtension(this);
            ColorScheme = gitExt;
            _extensions.Add(gitExt);

            _extensions.Add(new FileWatchExtension(this));

            _table = new ScfeTable(this);
            InstallOnActionMap(_table.ActionMap);
            _table.ActionOnComponent += (o, args) => HandlerOpenFileInTable(_table.FocusedElement, args.Graphics);

            _table.AddColumn(new IndicatorColumnType<File>());

            var columnsAvailable = _extensions.SelectMany(e => e.GetColumns()).ToList();
            foreach (var colName in columnsToAdd)
            {
                var col = columnsAvailable.FirstOrDefault(c => c.GetTitle(_table, int.MaxValue) == colName);
                if (col != null)
                    _table.AddColumn(col);
            }

            _wrapper.AddComponent(_table, 0);
            ((SwitcherStrategy) _wrapper.Strategy).SwitchToComponentWithHint(0, null);

            _tableInputMap = new SwappableAddinHierDic<KeyStroke, string>(_table.InputMap);
            _table.InputMap = _tableInputMap;

            InstallExtensionsOnFileOptionsPanel();

            SwitchToFolder(CurrentDir);

            ChangeModeTo(NavigationMode.NavMode, null);
        }


        private void InstallExtensionsOnFileOptionsPanel()
        {
            FileOption.Options.AddRange(_extensions.SelectMany(e => e.GetFilesOptions()));
            FileOption.OptionsForCurrentDirectory.AddRange(_extensions.SelectMany(e => e.GetCurrDirOptions()));
        }

        private void InstallOnActionMap(AbstractHierarchicalDictionary<string, Action<object, ActionEventArgs>> map)
        {
            map.Put(ScfeActions.GoDownFast, (sender, args) =>
            {
                var dest = _table.FocusedIndex + _table.Height - 1;
                if (dest > _table.Data.Count) dest = _table.Data.Count - 1;

                _table.FocusIndex(dest, args.Graphics);
            });
            map.Put(
                ScfeActions.GoUpFast, (sender, args) =>
                {
                    var dest = _table.FocusedIndex - _table.Height;
                    if (dest < 0) dest = 0;

                    _table.FocusIndex(dest, args.Graphics);
                }
            );
            map.Put(StandardActionNames.MoveLeft, (sender, args) =>
                {
                    var parent = CurrentDir.GetParent();
                    ResetTextBox(args.Graphics);
                    if (parent != null)
                        SwitchToFolder(parent, args.Graphics, CurrentDir);
                }
            );
            map.Put(
                ScfeActions.ToggleSelection, (o, args) =>
                {
                    for (int i = 0; i < _table.Data.Count; i++)
                    {
                        _table.TrySelect(i);
                    }

                    _table.Validate();
                    _table.Print(args.Graphics);
                }
            );
            map.Put(
                ScfeActions.SelectAll, (o, args) =>
                {
                    _table.ResetSelection();
                    for (int i = 0; i < _table.Data.Count; i++)
                    {
                        _table.TrySelect(i);
                    }

                    _table.Validate();
                    _table.Print(args.Graphics);
                });
            map.Put(
                StandardActionNames.SecondaryAction, (sender, args) =>
                {
                    OpenActionSecondaryAction(_table.SelectedElements.Count > 0
                        ? _table.SelectedElements
                        : new[] {_table.FocusedElement}.ToImmutableList(), args.Graphics);
                }
            );
            map.Put(
                StandardActionNames.SelectAction, (sender, args) =>
                {
                    _table.TrySelect(_table.FocusedIndex);
                    _table.ValidateAndPrint(_table.FocusedIndex, args.Graphics);
                }
            );

            foreach (var (actionName, action) in _extensions.SelectMany(e => e.GetActions()))
                map.Put(actionName, action);
        }

        public void OpenActionSecondaryAction(ImmutableList<File> files, GraphicsContext g1)
        {
            if (files != null && _table.FocusedElement == null)
                return;

            if (TextBoxHandler != null)
            {
                CancelTextBoxHandler(g1);
            }

            TextBox.Focusable = false;

            var fop = new FileOptionsPanel(files, _cons.InputMap);
            var container = new BoxContainer(fop, LineStyle.Dotted);
            fop.RemovalCallback = (actionToDo, g) =>
            {
                ((SwitcherStrategy) _wrapper.Strategy).SwitchToComponentWithHint(0, g);
                _wrapper.RemoveComponent(container);
                _wrapper.Validate();
                _wrapper.Print(g);
                TextBox.Focusable = true;

                if (actionToDo != null)
                    _table.ActionMap.Get(actionToDo)?.Invoke(null, new ActionEventArgs(_wrapper, null, g));
            };
            _wrapper.AddComponent(container, 1);
            ((SwitcherStrategy) _wrapper.Strategy).SwitchToComponentWithHint(1, g1);
            _wrapper.Validate();
            _wrapper.Print(g1);
        }

        private void SetupTextBoxAction(Action<object, ActionEventArgs> handler, Action<GraphicsContext> cancelHandler,
            string message, bool hideText, GraphicsContext g)
        {
            TextBoxHandler = handler;
            _textBoxCancelHandler = cancelHandler;
            TextBox.Focusable = true;
            _table.SetFocused(false, g);
            ShowHelpMessage(message, g, ConsoleColor.Yellow);
            ResetTextBox(g, !hideText);
            if (hideText)
            {
                TextBox.HideText = true;
                TextBox.Print(Math.Max(TextBox.Width - TextBox.DisplayedText.Length - 5, 0), g);
            }

            TextBox.SetFocused(true, g);
        }

        private void ResetFocusableStates(GraphicsContext g)
        {
            TextBox.Focusable = CurrentMode.SearchEnabled;
            TextBox.SetFocused(false, g);
            _table.SetFocused(true, g);
        }

        public void ResetTextBox(GraphicsContext g, bool reprint = true)
        {
            TextBox.Text = "";
            TextBox.CaretPosition = 0;
            TextBox.HideText = false;
            TextBox.Parent.Validate();
            if (reprint)
                TextBox.Print(Math.Max(TextBox.Width - TextBox.DisplayedText.Length - 5, 0), g);
        }


        private void AddMainBindings()
        {
            _main.InputMap.Put(new KeyStroke(ConsoleKey.PageDown, null, null, null), ScfeActions.GoDownFast);
            _main.InputMap.Put(new KeyStroke(ConsoleKey.PageUp, null, null, null), ScfeActions.GoUpFast);
            _main.InputMap.Put(new KeyStroke(ConsoleKey.F10, null, null, null), "RESETALL");
            _main.ActionMap.Put("RESETALL", (o, args) =>
            {
                _cons.Validate();
                var old = _cons.ClearAreaBeforePrint;
                _cons.ClearAreaBeforePrint = true;
                _cons.Print(args.Graphics);
                _cons.ClearAreaBeforePrint = old;
            });
        }

        public void HandlerOpenFileInTable(File f, GraphicsContext g)
        {
            if (f != null)
            {
                if (f.IsFolder())
                {
                    ResetTextBox(g);
                    SwitchToFolder(f, g);
                }
                else
                {
                    f.Open();
                    ShowHelpMessage("Opening file " + f.GetFileName(), g);
                }
            }
        }


        internal void SwitchToFolder(File file, GraphicsContext g = null, File fileToFocus = null)
        {
            var children = file.GetChildren();
            var newFiles = children.Where(_filter.GoesThrough).OrderBy(t => t,
                    UseReverseSorting ? FileSorting.ReversedComparer : FileSorting.Comparer)
                .ToImmutableList();
            var isFiltered = newFiles.Count != children.Where(_noSearchFilter.GoesThrough).Count();

            var oldFolder = CurrentDir;

            try
            {
                CurrentDir = file;
                var relPath = CurrentDir.GetRelativePath(File.UserHome);
                if (relPath == null || relPath.StartsWith(".."))
                    _filePath.Text = file.Path;
                else
                    _filePath.Text = "~" + Path.DirectorySeparatorChar + relPath;
                _elementsCount.Foreground = isFiltered ? ConsoleColor.Red : (ConsoleColor?) null;
                _elementsCount.Text = "[" + newFiles.Count + (isFiltered ? "*" : "") + "]";
                _table.TrackTableChanges = false;
                _table.QuickClearOnNextPrint = true;
                if (_table.Data == null)
                    _table.Data = _files;
                else
                    _files.Clear();
                if (newFiles.Count > 0)
                    foreach (var f in newFiles)
                        _files.Add(f);
                else
                    _files.Add(null);

                _cons?.SetTitle("SCFE - " + CurrentDir.Path);
            }
            finally
            {
                _table.TrackTableChanges = true;
            }

            if (g != null)
            {
                _filePath.Parent.Validate();
                _filePath.Print(g);
                _elementsCount.Print(g);

                if (TextBoxHandler != null)
                {
                    CancelTextBoxHandler(g);
                }

                var doFocus = _table.IsFocused();

                _table.RefreshColumnSizes();

                if (fileToFocus != null && _table.Data.Contains(fileToFocus))
                    _table.FocusedElement = fileToFocus;
                else
                    _table.FocusedIndex = 0;

                _table.ResetSelection();
                _table.Validate();
                _table.Print(g);
                if (doFocus)
                    _table.SetFocused(true, g);

                //g.ClearArea(_filePath.X, _filePath.Y, oldPathText.Length, 1);
                _filePath.Print(g);
            }

            OnFolderViewChanged?.Invoke(this,
                new FolderViewChangedEventArgs(oldFolder != CurrentDir, oldFolder, CurrentDir, g));
        }

        public void Show()
        {
            _cons = new ConsoleParent(_main)
            {
                ExceptionHandler = (e, g) => { ShowHelpMessage(e.Message.Replace("\n", " - "), g, ConsoleColor.Red); }
            };
            _cons.Validate();
            // Only once the table is actually added
            _cons.FocusFirst();
            _cons.Print();
            _cons.SetTitle("SCFE - " + CurrentDir.Path);
        }

        public Parent GetBaseParent()
        {
            return _main;
        }

        public void AddTask(Func<ManagedTask<string>, TaskResult> taskFun)
        {
            Tasks.AddTask(taskFun);
        }

        public void AddTask(ManagedTask<string> task)
        {
            Tasks.AddTask(task);
        }

        public void ChangeModeTo(NavigationMode mode, GraphicsContext g)
        {
            CurrentMode = mode;
            _tableInputMap.AddInDictionary = mode.Bindings;
            PrintModeInformation(mode, g);
            TextBox.Focusable = mode.SearchEnabled;
            _filterOnBoxInput = mode.SearchEnabled;
        }

        public void PrintModeInformation(NavigationMode mode, [CanBeNull] GraphicsContext g)
        {
            _mode.Text = mode.Name;
            _mode.Foreground = mode.Color;
            if (g != null)
                _mode.Print(g);
        }

        public void ShowHelpMessage(string s, GraphicsContext g, ConsoleColor? color = null)
        {
            _help.Text = s;
            _help.Foreground = color;
            _help.Parent.Validate();
            if (g != null)
                _help.Parent.Print(g);
        }

        public void RefreshTableComponent(GraphicsContext g)
        {
            if (_table.Visible)
            {
                SwitchToFolder(CurrentDir, g, FocusedElement);
            }
        }

        public void Request(string message, GraphicsContext g, Action<string, GraphicsContext, Action> callback,
            bool hideText = false,
            Action<GraphicsContext> cancelledCallback = null)
        {
            SetupTextBoxAction((o1, eventArgs) =>
            {
                bool finished = false;

                void FinishAction()
                {
                    finished = true;
                    TextBoxHandler = null;
                    _textBoxCancelHandler = null;
                    ResetTextBox(eventArgs.Graphics);
                    ResetFocusableStates(eventArgs.Graphics);
                }

                try
                {
                    string s = TextBox.Text;
                    ResetTextBox(eventArgs.Graphics);
                    callback(s, eventArgs.Graphics, FinishAction);
                }
                catch (Exception)
                {
                    if (!finished)
                    {
                        FinishAction();
                    }

                    throw;
                }
            }, cancelledCallback, message, hideText, g);
        }

        private void CancelTextBoxHandler(GraphicsContext g)
        {
            _textBoxCancelHandler?.Invoke(g);
            _textBoxCancelHandler = null;
            TextBoxHandler = null;
            ResetTextBox(g);
            ResetFocusableStates(g);
            ShowHelpMessage("Cancelled.", g);
        }

        public string RequestSync(string message, bool hideText = false)
        {
            string result = null;
            CountdownEvent cde = new CountdownEvent(1);
            _cons.DoGraphicsAndWait(g =>
            {
                Request(message, g, (s, context, finishFunc) =>
                {
                    result = s;
                    cde.Signal();
                    finishFunc();
                }, hideText, context => { cde.Signal(); });
            });
            cde.Wait();
            return result;
        }

        public void ShowHelpMessageLater(string message, ConsoleColor? color = null)
        {
            _cons.DoGraphicsLater(g => ShowHelpMessage(message, g, color));
        }

        public void DoGraphicsAndWait(Action<GraphicsContext> action)
        {
            _cons.DoGraphicsAndWait(action);
        }

        public void DoGraphicsLater(Action<GraphicsContext> action)
        {
            _cons.DoGraphicsLater(action);
        }

        public void FireFileChangedEvent(object sender, FileChangedEventArgs fileChangedEventArgs)
        {
            OnFileChanged?.Invoke(this, fileChangedEventArgs);
        }

        public void FirePreRefreshEvent(object sender)
        {
            OnPreRefresh?.Invoke(sender, EventArgs.Empty);
        }
    }
}
