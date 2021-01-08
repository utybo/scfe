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
using System.IO;
using System.Linq;
using Viu;
using Viu.Table;

namespace SCFE
{
    public class BaseExtension : IScfeExtension
    {
        private ScfeApp _app;

        public BaseExtension(ScfeApp app)
        {
            _app = app;
        }

        public IEnumerable<ColumnType<File>> GetColumns()
        {
            return new List<ColumnType<File>>
            {
                new BasicColumnType<File>("name", file => file?.GetFileName() ?? "<nothing to show>",
                        file => file != null ? _app.ColorScheme?.GetColorForFile(file) : ConsoleColor.DarkGray)
                    {GrowPriority = 10, ShrinkPriority = 10},
                new MultistateColumnType<File>(new[] {"sz", "size"},
                    f => new[]
                    {
                        f?.GetSizeString(4),
                        f?.GetSizeString(5),
                        f?.GetSizeString(6)
                    },
                    data => data.Any(f => f != null)
                ) {HAlign = HorizontalAlignment.Right},
                new MultistateColumnType<File>(new[] {"dt", "date"},
                    f => ScfeUtils.GetHumanReadableDate(f.GetModificationDate()),
                    data => data.Any(f => f != null)) {HAlign = HorizontalAlignment.Right}
            };
        }

        public Dictionary<string, Action<object, ActionEventArgs>> GetActions()
        {
            return new Dictionary<string, Action<object, ActionEventArgs>>
            {
                {
                    StandardActionNames.MoveRight,
                    (sender, args) => _app.HandlerOpenFileInTable(_app.FocusedElement, args.Graphics)
                },
                {
                    ScfeActions.ChangeMode, (sender, args) =>
                    {
                        var i = NavigationMode.NavigationModes.IndexOf(_app.CurrentMode);
                        _app.ChangeModeTo(
                            NavigationMode.NavigationModes[(i + 1) % NavigationMode.NavigationModes.Count],
                            args.Graphics);
                    }
                },

                {
                    ScfeActions.ToggleShowHiddenFiles, (sender, args) =>
                    {
                        var focused = _app.FocusedElement;
                        _app.ShowHiddenFiles = !_app.ShowHiddenFiles;
                        if (_app.ShowHiddenFiles)
                            _app.ShowHelpMessage("Hidden files are now shown", args.Graphics);
                        else
                            _app.ShowHelpMessage("Hidden files are now hidden", args.Graphics);
                        _app.SwitchToFolder(_app.CurrentDir, args.Graphics, focused);
                    }
                },

                {
                    ScfeActions.CreateFile, (o, args) =>
                    {
                        _app.Request("[New] Enter new file name (Esc to cancel)", args.Graphics, (s, g, finishAction) =>
                        {
                            if (string.IsNullOrWhiteSpace(s))
                                return;
                            var newFile = _app.CurrentDir.GetChild(s);
                            if (newFile.Exists())
                            {
                                _app.ShowHelpMessage("File already exists, choose another name", g,
                                    ConsoleColor.Red);
                                return;
                            }

                            finishAction();

                            FileWatchExtension.TempIgnoreList.Add(newFile);
                            newFile.CreateFile();
                            _app.ShowHelpMessage("File created.", args.Graphics, ConsoleColor.Green);
                            _app.FireFileChangedEvent(this, new FileChangedEventArgs(new[] {newFile}, g));

                            _app.SwitchToFolder(_app.CurrentDir, g, newFile);
                        });
                    }
                },
                {
                    ScfeActions.CreateFolder, (o, args) =>
                    {
                        _app.Request("[NewF] Enter new folder name (Esc to cancel)", args.Graphics, (s, g, finish) =>
                        {
                            if (string.IsNullOrWhiteSpace(s))
                                return;
                            var newFile = _app.CurrentDir.GetChild(s);
                            if (newFile.Exists())
                            {
                                _app.ShowHelpMessage("Folder already exists, choose another name", g,
                                    ConsoleColor.Red);
                                return;
                            }

                            finish();
                            FileWatchExtension.TempIgnoreList.Add(newFile);
                            newFile.CreateDirectory();

                            _app.ShowHelpMessage("Folder created and opened.", g, ConsoleColor.Green);
                            _app.FireFileChangedEvent(this, new FileChangedEventArgs(new[] {newFile}, g));
                            _app.SwitchToFolder(newFile, g);
                        });
                    }
                },
                {
                    ScfeActions.Rename, (o, args) =>
                    {
                        var files = _app.SelectedElements.Count == 0
                            ? new[] {_app.FocusedElement}.ToImmutableList()
                            : _app.SelectedElements;
                        if (files.Count != 1)
                        {
                            _app.ShowHelpMessage("Cannot rename more than one file at the same time", args.Graphics,
                                ConsoleColor.Red);
                            return;
                        }

                        _app.Request($"[Rename] Enter new name for {files[0].GetFileName()} (Esc to cancel)",
                            args.Graphics,
                            (s, g, finish) =>
                            {
                                finish();
                                if (string.IsNullOrWhiteSpace(s))
                                    return;
                                var moveTo = files[0].GetSibling(s);
                                if (moveTo.Exists())
                                {
                                    _app.ShowHelpMessage("A file already exists with this name", g,
                                        ConsoleColor.Red);
                                    return;
                                }

                                try
                                {
                                    FileWatchExtension.TempIgnoreList.Add(moveTo);
                                    files[0].MoveTo(moveTo);
                                    _app.ShowHelpMessage("File renamed successfully", args.Graphics,
                                        ConsoleColor.Green);
                                    _app.FireFileChangedEvent(this,
                                        new FileChangedEventArgs(new[] {files[0], moveTo}, args.Graphics));

                                    _app.SwitchToFolder(_app.CurrentDir, args.Graphics, moveTo);
                                }
                                catch (Exception e)
                                {
                                    if (e is IOException)
                                        _app.ShowHelpMessage(e.Message, args.Graphics, ConsoleColor.Red);
                                    throw;
                                }
                            });
                    }
                },
                {
                    ScfeActions.GoToFolder, (o, args) =>
                    {
                        _app.Request("[Go-To] Enter destination folder (Esc to cancel)", args.Graphics,
                            (s, g, finish) =>
                            {
                                finish();

                                if (string.IsNullOrWhiteSpace(s))
                                {
                                    _app.ShowHelpMessage("Cancelled", g);
                                    return;
                                }

                                var goTo = new File(s);
                                if (!goTo.Exists())
                                {
                                    _app.ShowHelpMessage("Destination does not exist", g,
                                        ConsoleColor.Red);
                                    return;
                                }

                                _app.ShowHelpMessage("Destination opened.", g, ConsoleColor.Green);
                                _app.HandlerOpenFileInTable(goTo, g);
                            });
                    }
                },
                {
                    ScfeActions.DeleteFile, (o, args) =>
                    {
                        var files = _app.SelectedElements.IsEmpty
                            ? new[] {_app.FocusedElement}.ToImmutableList()
                            : _app.SelectedElements;
                        var folderFrom = _app.CurrentDir;
                        _app.Request(files.Count == 1
                                ? "[Delete] To delete file " + files[0].GetFileName() + " enter 'yes' (Esc to cancel)"
                                : "[Delete] To delete " + files.Count + " files enter 'yes' (Esc to cancel)",
                            args.Graphics, (s, context, finish) =>
                            {
                                finish();
                                if (s == "yes")
                                {
                                    var task = new ManagedTask<string>(tsk =>
                                    {
                                        try
                                        {
                                            foreach (var f in files)
                                                f.Delete(true);

                                            return new TaskResult(true, "Files deleted");
                                        }
                                        catch (Exception e)
                                        {
                                            return new TaskResult(false, "Error: " + e.Message);
                                        }
                                    });
                                    task.AddCallback((managedTask, result) =>
                                    {
                                        _app.DoGraphicsLater(g =>
                                        {
                                            if (_app.CurrentDir == folderFrom)
                                            {
                                                _app.FireFileChangedEvent(this,
                                                    new FileChangedEventArgs(files, args.Graphics));
                                            }
                                        });
                                    });
                                    _app.AddTask(task);
                                    _app.ShowHelpMessage(files.Count == 1
                                        ? "Deleting " + files[0].GetFileName() + "..."
                                        : "Deleting " + files.Count + " files...", args.Graphics);
                                }
                                else
                                {
                                    _app.ShowHelpMessage("Cancelled", args.Graphics);
                                }
                            });
                    }
                },
                {
                    ScfeActions.CopyFile, (o, args) =>
                    {
                        var files = _app.SelectedElements.IsEmpty
                            ? (IEnumerable<File>) new[] {_app.FocusedElement}
                            : _app.SelectedElements;
                        _app.ClipboardContent = files.ToImmutableList();
                        _app.ClipboardMode = FileTransferMode.Copy;
                        _app.ShowHelpMessage(_app.ClipboardContent.Count + " files copied to clipboard", args.Graphics);
                    }
                },
                {
                    ScfeActions.CutFile, (o, args) =>
                    {
                        var files = _app.SelectedElements.IsEmpty
                            ? (IEnumerable<File>) new[] {_app.FocusedElement}
                            : _app.SelectedElements;
                        _app.ClipboardContent = files.ToImmutableList();
                        _app.ClipboardMode = FileTransferMode.Cut;
                        _app.ShowHelpMessage(_app.ClipboardContent.Count + " files cut to clipboard", args.Graphics);
                    }
                },
                {
                    ScfeActions.PasteFile, (o, args) =>
                    {
                        var filesToPaste = _app.ClipboardContent;
                        var destinationFolder = _app.CurrentDir;
                        var mode = _app.ClipboardMode;
                        var task = new ManagedTask<string>(tsk =>
                        {
                            try
                            {
                                if (filesToPaste.Count == 1)
                                    FileWatchExtension.TempIgnoreList.Add(
                                        destinationFolder.GetChild(filesToPaste[0].GetFileName()));
                                foreach (var f in filesToPaste)
                                {
                                    if (mode == FileTransferMode.Copy)
                                        f.CopyTo(destinationFolder.GetChild(f.GetFileName()));
                                    else
                                        f.MoveTo(destinationFolder.GetChild(f.GetFileName()));
                                }

                                return new TaskResult(true, "Successfully "
                                                            + (mode == FileTransferMode.Copy ? "copied " : "moved ")
                                                            + (filesToPaste.Count == 1
                                                                ? "1 file"
                                                                : filesToPaste.Count + " files"));
                            }
                            catch (Exception e)
                            {
                                return new TaskResult(false, "Error: " + e.Message);
                            }
                        });
                        task.AddCallback((managedTask, result) =>
                        {
                            _app.DoGraphicsLater(g =>
                            {
                                if (_app.CurrentDir == destinationFolder)
                                {
                                    _app.FireFileChangedEvent(this,
                                        new FileChangedEventArgs(filesToPaste.Add(destinationFolder), args.Graphics));

                                    if (filesToPaste.Count == 1)
                                        _app.SwitchToFolder(_app.CurrentDir, g,
                                            destinationFolder.GetChild(filesToPaste[0].GetFileName()));
                                }
                            });
                        });
                        _app.AddTask(task);
                        _app.ShowHelpMessage(
                            (mode == FileTransferMode.Copy ? "Copying " : "Moving ")
                            + (filesToPaste.Count == 1 ? "1 file..." : filesToPaste.Count + " files..."),
                            args.Graphics);
                    }
                },
                {
                    ScfeActions.Refresh, (o, args) =>
                    {
                        _app.ShowHelpMessage("Refreshing folder view...", args.Graphics);
                        _app.FirePreRefreshEvent(this);
                        _app.SwitchToFolder(_app.CurrentDir, args.Graphics, _app.FocusedElement);
                        _app.ShowHelpMessage("Folder refreshed", args.Graphics, ConsoleColor.DarkGreen);
                    }
                },
                {ScfeActions.CurrDirOptions, (o, args) => _app.OpenActionSecondaryAction(null, args.Graphics)},
                {
                    ScfeActions.ChangeSort, (o, args) =>
                    {
                        int nextIndex = (_app.FileSorters.IndexOf(_app.FileSorting) + 1) % _app.FileSorters.Count;
                        _app.FileSorting = _app.FileSorters[nextIndex];
                        var ordName = _app.UseReverseSorting
                            ? _app.FileSorting.ReversedComparerOrder
                            : _app.FileSorting.NormalComparerOrder;
                        _app.ShowHelpMessage($"Now sorting {_app.FileSorting.Name} ({ordName})", args.Graphics,
                            ConsoleColor.Magenta);
                        _app.SwitchToFolder(_app.CurrentDir, args.Graphics, _app.FocusedElement);
                    }
                },
                {
                    ScfeActions.ToggleSortOrder, (o, args) =>
                    {
                        _app.UseReverseSorting = !_app.UseReverseSorting;
                        var ordName = _app.UseReverseSorting
                            ? _app.FileSorting.ReversedComparerOrder
                            : _app.FileSorting.NormalComparerOrder;
                        _app.ShowHelpMessage($"Now sorting {_app.FileSorting.Name} ({ordName})", args.Graphics,
                            ConsoleColor.Magenta);
                        _app.SwitchToFolder(_app.CurrentDir, args.Graphics, _app.FocusedElement);
                    }
                }
            };
        }

        public IEnumerable<FileOption> GetCurrDirOptions()
        {
            return new List<FileOption>
            {
                new FileOption
                {
                    Title = "Create file",
                    ActionName = ScfeActions.CreateFile
                },
                new FileOption
                {
                    Title = "Create folder",
                    ActionName = ScfeActions.CreateFolder
                },
                new FileOption
                {
                    Title = "Paste",
                    ActionName = ScfeActions.PasteFile
                },
                new FileOption
                {
                    Title = "Go to parent",
                    ActionName = StandardActionNames.MoveLeft
                },
                new FileOption
                {
                    Title = "Quick Go-To",
                    ActionName = ScfeActions.GoToFolder
                },
                new FileOption
                {
                    Title = "Refresh",
                    ActionName = ScfeActions.Refresh
                },
                new FileOption
                {
                    Title = "Change sorting method",
                    ActionName = ScfeActions.ChangeSort
                },
                new FileOption
                {
                    Title = "Reverse sorting order",
                    ActionName = ScfeActions.ToggleSortOrder
                },
                new FileOption
                {
                    Title = "Show/Hide hidden files",
                    ActionName = ScfeActions.ToggleShowHiddenFiles
                },
                new FileOption
                {
                    Title = "Select all",
                    ActionName = ScfeActions.SelectAll
                },
                new FileOption
                {
                    Title = "Toggle selection",
                    ActionName = ScfeActions.ToggleSelection
                },
                new FileOption
                {
                    Title = "Toggle mode between NAV and SEA",
                    ActionName = ScfeActions.ChangeMode
                }
            };
        }

        public IEnumerable<FileOption> GetFilesOptions()
        {
            return new List<FileOption>
            {
                new FileOption
                {
                    Title = "Open",
                    ActionName = StandardActionNames.BaseAction,
                    CanActionBeApplied = FileOption.OnlyIfSingleFile
                },
                new FileOption
                {
                    Title = "Copy",
                    ActionName = ScfeActions.CopyFile,
                    CanActionBeApplied = FileOption.Always
                },
                new FileOption
                {
                    Title = "Cut",
                    ActionName = ScfeActions.CutFile,
                    CanActionBeApplied = FileOption.Always
                },
                new FileOption
                {
                    Title = "Rename",
                    ActionName = ScfeActions.Rename,
                    CanActionBeApplied = FileOption.OnlyIfSingleFile
                },
                new FileOption
                {
                    Title = "Delete",
                    ActionName = ScfeActions.DeleteFile,
                    CanActionBeApplied = FileOption.Always
                },
                new FileOption
                {
                    Title = "Select/Deselect",
                    ActionName = StandardActionNames.SelectAction,
                    CanActionBeApplied = FileOption.OnlyIfSingleFile
                }
            };
        }
    }
}
