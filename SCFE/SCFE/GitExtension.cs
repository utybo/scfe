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
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Viu;
using Viu.Table;

namespace SCFE
{
    public class GitExtension : FileColorScheme, IScfeExtension, IDisposable
    {
        /// <summary>
        ///     List of FileStatuses that correspond to a change in the working directory (i.e. some changes are present in
        ///     the working directory, but are not registered in the index nor in the HEAD)
        /// </summary>
        private static readonly ImmutableList<FileStatus> WorkDirChangeStatuses = new[]
        {
            FileStatus.NewInWorkdir,
            FileStatus.RenamedInWorkdir,
            FileStatus.ModifiedInWorkdir,
            FileStatus.DeletedFromWorkdir,
            FileStatus.TypeChangeInWorkdir
        }.ToImmutableList();

        /// <summary>
        ///     List of FileStatuses that correspond to a change in the index (i.e. some changes are present in the working
        ///     directory and in the index, but not in the HEAD)
        /// </summary>
        private static readonly ImmutableList<FileStatus> IndexChangeStatuses = new[]
        {
            FileStatus.NewInIndex,
            FileStatus.RenamedInIndex,
            FileStatus.ModifiedInIndex,
            FileStatus.DeletedFromIndex,
            FileStatus.TypeChangeInIndex
        }.ToImmutableList();

        private readonly ScfeApp _app;

        private readonly Dictionary<string, DirectoryStatus> _cachedDirStatus =
            new Dictionary<string, DirectoryStatus>();

        private readonly Dictionary<string, bool> _cachedIgnoredStatus = new Dictionary<string, bool>();
        private Repository _cachedRepo;
        private string _cachedRepoPath;
        private RepositoryStatus _repoStatus;

        public GitExtension(ScfeApp app)
        {
            _app = app;
            _app.OnPreRefresh += (sender, args) => RefreshRepository(null);
            _app.OnFileChanged += (sender, args) =>
            {
                if (!args.Files.Any(f => f.Exists()))
                    return;

                RefreshRepository(null);
                RefreshRepository(_app.CurrentDir);
            };
        }

        public IEnumerable<ColumnType<File>> GetColumns()
        {
            return new List<ColumnType<File>>
            {
                new MultistateColumnType<File>(new[] {"git"}, file =>
                {
                    if (file == null)
                        return new[] {""};

                    var relPath = GetPathRelativeToRepo(file);

                    var status = CachedFileStatus(relPath);
                    if (status == 0 && file.IsFolder())
                    {
                        if (!_cachedDirStatus.ContainsKey(relPath)) ComputeStatusAndCache(file, relPath);

                        var st = _cachedDirStatus[relPath];
                        if (st != DirectoryStatus.Unchanged)
                            return DirStatusToString(st);
                    }

                    var txt = StatusToString(status).ToArray();

                    return _cachedRepo == null ? new[] {"N/A"} : txt;
                }, data =>
                {
                    RefreshRepository(_app.CurrentDir);
                    return _cachedRepo != null;
                }, GetColorForFile)
            };
        }

        public Dictionary<string, Action<object, ActionEventArgs>> GetActions()
        {
            return new Dictionary<string, Action<object, ActionEventArgs>>
            {
                {
                    ScfeActions.GitStage, (o, args) =>
                    {
                        RefreshRepository(_app.CurrentDir);

                        if (_cachedRepo == null)
                        {
                            _app.ShowHelpMessage("[git] Not in a Git repository", args.Graphics, ConsoleColor.Red);
                            return;
                        }

                        var toAdd = _app.SelectedElements.Count > 0
                            ? _app.SelectedElements
                            : new List<File> {_app.FocusedElement}.ToImmutableList();

                        Commands.Stage(_cachedRepo, toAdd.Select(GetPathRelativeToRepo));

                        RefreshRepository(null);
                        RefreshRepository(_app.CurrentDir);
                        _app.RefreshTableComponent(args.Graphics);
                        _app.ShowHelpMessage("[git] " + (toAdd.Count > 1
                                                 ? toAdd.Count + " files staged"
                                                 : toAdd[0].GetFileName() + " staged"), args.Graphics,
                            ConsoleColor.Green);
                    }
                },
                {
                    ScfeActions.GitUnstage, (o, args) =>
                    {
                        RefreshRepository(_app.CurrentDir);

                        if (_cachedRepo == null)
                        {
                            _app.ShowHelpMessage("[git] Not in a Git repository", args.Graphics, ConsoleColor.Red);
                            return;
                        }

                        var toAdd = _app.SelectedElements.Count > 0
                            ? _app.SelectedElements
                            : new List<File> {_app.FocusedElement}.ToImmutableList();

                        Commands.Unstage(_cachedRepo, toAdd.Select(GetPathRelativeToRepo));

                        RefreshRepository(null);
                        RefreshRepository(_app.CurrentDir);
                        _app.RefreshTableComponent(args.Graphics);
                        _app.ShowHelpMessage("[git] " + (toAdd.Count > 1
                                                 ? toAdd.Count + " files unstaged"
                                                 : toAdd[0].GetFileName() + " unstaged"), args.Graphics,
                            ConsoleColor.Green);
                    }
                },
                {
                    ScfeActions.GitInit,
                    (o, args) =>
                    {
                        RefreshRepository(_app.CurrentDir);
                        if (_cachedRepo != null)
                        {
                            _app.ShowHelpMessage("[git] Repository already exists in this folder (or a parent)",
                                args.Graphics, ConsoleColor.Red);
                            return;
                        }

                        Repository.Init(_app.CurrentDir.FullPath, false);
                        RefreshRepository(null);
                        RefreshRepository(_app.CurrentDir);

                        _app.RefreshTableComponent(args.Graphics);
                        _app.ShowHelpMessage("[git] Repository initialized successfully", args.Graphics,
                            ConsoleColor.Green);
                    }
                },
                {
                    ScfeActions.GitCommit,
                    (o, args) =>
                    {
                        RefreshRepository(_app.CurrentDir);
                        if (_cachedRepo == null)
                        {
                            _app.ShowHelpMessage("[git] Not in a git repository", args.Graphics, ConsoleColor.Red);
                            return;
                        }

                        _app.Request("[git] Please choose a commit message (Enter to confirm, Esc to cancel)",
                            args.Graphics, (s, context, finishFunc) =>
                            {
                                finishFunc();
                                RefreshRepository(_app.CurrentDir);
                                if (_cachedRepo == null)
                                {
                                    _app.ShowHelpMessage("[git] Not in a git repository", args.Graphics,
                                        ConsoleColor.Red);
                                    return;
                                }

                                Signature sign = _cachedRepo.Config.BuildSignature(DateTimeOffset.Now);
                                _cachedRepo.Commit(s, sign, sign);
                                _app.ShowHelpMessage("[git] Changes committed successfully", args.Graphics,
                                    ConsoleColor.Green);
                                RefreshRepository(null);
                                RefreshRepository(_app.CurrentDir);
                                _app.RefreshTableComponent(args.Graphics);
                            }
                        );
                    }
                },
                {
                    ScfeActions.GitPush, (o, args) =>
                    {
                        _app.AddTask(tk =>
                        {
                            _app.ShowHelpMessageLater("[git] Pushing...");
                            bool forceFail = false;
                            var opt = new PushOptions
                            {
                                CredentialsProvider = (url, fromUrl, types) =>
                                {
                                    if (types.HasFlag(SupportedCredentialTypes.UsernamePassword))
                                    {
                                        string uname = _app.RequestSync("[git] (Push) Username?");
                                        if (uname == null)
                                        {
                                            forceFail = true;
                                            return null;
                                        }

                                        string pw = _app.RequestSync("[git] (Push) Password?", true);
                                        if (pw == null)
                                        {
                                            forceFail = true;
                                            return null;
                                        }

                                        return new UsernamePasswordCredentials {Username = uname, Password = pw};
                                    }

                                    return new DefaultCredentials();
                                },
                                OnPushTransferProgress = (current, total, bytes) =>
                                {
                                    _app.ShowHelpMessageLater("[git] Push progress: " + current + "/" + total);
                                    return !forceFail;
                                },
                                OnNegotiationCompletedBeforePush = updates => true,
                                OnPackBuilderProgress = (stage, current, total) =>
                                {
                                    _app.ShowHelpMessageLater(
                                        "[git] Preparing push: " +
                                        (stage == PackBuilderStage.Deltafying ? "deltafying" : "counting") + " " +
                                        current + "/" + total);
                                    return !forceFail;
                                },
                                OnPushStatusError = errors =>
                                {
                                    _app.ShowHelpMessageLater(
                                        "[git] Error: " + errors.Reference + " " + errors.Message,
                                        ConsoleColor.Red);
                                }
                            };
                            try
                            {
                                _cachedRepo.Network.Push(_cachedRepo.Head, opt);
                                return new TaskResult(true, "[git] Push finished successfully");
                            }
                            catch (Exception e)
                            {
                                if (forceFail)
                                    return new TaskResult(false, "[git] Push aborted");
                                return new TaskResult(false, "[git] Error: " + e.Message);
                            }
                        });
                    }
                },
                {
                    ScfeActions.GitPull, (o, args) =>
                    {
                        _app.AddTask(tk =>
                        {
                            _app.ShowHelpMessageLater("[git] Pulling...");
                            bool forceFail = false;
                            var opt = new PullOptions
                            {
                                FetchOptions = new FetchOptions
                                {
                                    CertificateCheck = (certificate, valid, host) => true, // dangerous
                                    CredentialsProvider = (url, fromUrl, types) =>
                                    {
                                        if (types.HasFlag(SupportedCredentialTypes.UsernamePassword))
                                        {
                                            string uname = _app.RequestSync("[git] (Pull) Username?");
                                            if (uname == null)
                                            {
                                                forceFail = true;
                                                return null;
                                            }

                                            string pw = _app.RequestSync("[git] (Pull) Password?", true);
                                            if (pw == null)
                                            {
                                                forceFail = true;
                                                return null;
                                            }

                                            return new UsernamePasswordCredentials {Username = uname, Password = pw};
                                        }

                                        return new DefaultCredentials();
                                    },
                                    OnTransferProgress = progress =>
                                    {
                                        _app.ShowHelpMessageLater(
                                            $"[git] Transfer status (objects): {progress.ReceivedObjects} received, {progress.IndexedObjects} indexed, {progress.TotalObjects} total");
                                        return !forceFail;
                                    },
                                    OnUpdateTips = (name, id, newId) =>
                                    {
                                        _app.ShowHelpMessageLater(
                                            $"[git] Reference {name} updated, {id.Sha} -> {newId.Sha}");
                                        return !forceFail;
                                    }
                                },
                                MergeOptions = new MergeOptions
                                {
                                    FailOnConflict = true,
                                    CommitOnSuccess = true,
                                    OnCheckoutProgress = (path, steps, totalSteps) =>
                                    {
                                        _app.ShowHelpMessageLater($"[git] Merging: {steps}/{totalSteps} steps");
                                    }
                                }
                            };
                            try
                            {
                                Signature sign = _cachedRepo.Config.BuildSignature(DateTimeOffset.Now);
                                Commands.Pull(_cachedRepo, sign, opt);
                                RefreshRepository(null);
                                RefreshRepository(_app.CurrentDir);
                                _app.DoGraphicsAndWait(g => _app.RefreshTableComponent(g));
                                return new TaskResult(true, "[git] Pull finished successfully");
                            }
                            catch (Exception e)
                            {
                                if (forceFail)
                                    return new TaskResult(false, "[git] Pull aborted");
                                return new TaskResult(false, "[git] Error: " + e.Message);
                            }
                        });
                    }
                },
                {
                    ScfeActions.GitClone,
                    (o, args) =>
                    {
                        _app.AddTask(tk =>
                        {
                            RefreshRepository(_app.CurrentDir);
                            if (_cachedRepo != null)
                            {
                                return new TaskResult(false,
                                    "[git] Cannot clone a git repository while in a git repository");
                            }

                            string cloneUri = _app.RequestSync("[git] (Clone) Clone URL?");
                            if (cloneUri == null)
                                return new TaskResult(false, "[git] Clone aborted");
                            string cloneInFolder =
                                _app.RequestSync(
                                    "[git] (Clone) Directory name for cloned repository? (Leave empty for clone in current dir)");
                            if (cloneInFolder == null)
                                return new TaskResult(false, "[git] Clone aborted");
                            File cloneIn;
                            if (string.IsNullOrWhiteSpace(cloneInFolder))
                            {
                                cloneIn = _app.CurrentDir;
                            }
                            else
                            {
                                cloneIn = _app.CurrentDir.GetChild(cloneInFolder);
                                if (!cloneIn.Exists())
                                    cloneIn.CreateDirectory(true);
                            }

                            bool forceFail = false;
                            Stopwatch timeSinceLastMsg = Stopwatch.StartNew();
                            var opt = new CloneOptions
                            {
                                Checkout = true,
                                IsBare = false,
                                RecurseSubmodules = true,
                                OnProgress = output =>
                                {
                                    var outVal = output.Replace("\n", "").Split('\r');
                                    _app.ShowHelpMessageLater(
                                        "[git] Clone progress: " +
                                        outVal[outVal.Length > 3 ? outVal.Length - 2 : outVal.Length - 1]);
                                    return !forceFail;
                                },
                                OnUpdateTips = (reference, oldId, newId) =>
                                {
                                    _app.ShowHelpMessageLater(
                                        $"[git] Clone reference {reference} updated: {oldId} -> {newId}");
                                    return !forceFail;
                                },
                                OnCheckoutProgress = (s, steps, totalSteps) =>
                                {
                                    _app.ShowHelpMessageLater(
                                        $"[git] Checkout progress: {steps}/{totalSteps} steps");
                                },
                                OnTransferProgress = progress =>
                                {
                                    if (progress.ReceivedObjects % 10 == 1 || timeSinceLastMsg.ElapsedMilliseconds > 30)
                                    {
                                        _app.ShowHelpMessageLater(
                                            $"[git] Transfer progress: {progress.ReceivedObjects} received, {progress.IndexedObjects} indexed, {progress.TotalObjects} total");
                                        timeSinceLastMsg.Restart();
                                    }

                                    return !forceFail;
                                },
                                CertificateCheck = (certificate, valid, host) => true, // This is dangerous
                                CredentialsProvider = (url, fromUrl, types) =>
                                {
                                    if (types.HasFlag(SupportedCredentialTypes.UsernamePassword))
                                    {
                                        string uname = _app.RequestSync("[git] (Clone) Username?");
                                        if (uname == null)
                                        {
                                            forceFail = true;
                                            return null;
                                        }

                                        string pw = _app.RequestSync("[git] (Clone) Password?", true);
                                        if (pw == null)
                                        {
                                            forceFail = true;
                                            return null;
                                        }

                                        return new UsernamePasswordCredentials {Username = uname, Password = pw};
                                    }

                                    return new DefaultCredentials();
                                }
                            };

                            try
                            {
                                _app.ShowHelpMessageLater("[git] Cloning...");
                                var path = Repository.Clone(cloneUri, cloneIn.FullPath, opt);

                                if (path != null)
                                {
                                    tk.AddCallback((tk1, tr) =>
                                        _app.DoGraphicsLater(g =>
                                        {
                                            var fpath = new File(path.TrimEnd(Path.DirectorySeparatorChar));
                                            if (fpath.GetFileName() == ".git")
                                                fpath = fpath.GetParent();
                                            _app.SwitchToFolder(fpath);
                                            _app.RefreshTableComponent(g);
                                        }));
                                    return new TaskResult(true, "[git] Repo cloned and opened successfully.");
                                }

                                return new TaskResult(true, "[git] Repo cloned successfully");
                            }
                            catch (Exception e)
                            {
                                return new TaskResult(false,
                                    forceFail ? "[git] Clone aborted" : "[git] Clone error: " + e.Message);
                            }
                        });
                    }
                }
            };
        }

        public IEnumerable<FileOption> GetCurrDirOptions()
        {
            return new[]
            {
                new FileOption
                {
                    Title = "(git) Initialize a repository here",
                    ActionName = ScfeActions.GitInit,
                    CanActionBeApplied = (list, o) =>
                        !IsGitRepository(new[] {_app.CurrentDir}.ToImmutableList(), null)
                },
                new FileOption
                {
                    Title = "(git) Create a commit",
                    ActionName = ScfeActions.GitCommit,
                    CanActionBeApplied = (list, o) => IsGitRepository(new[] {_app.CurrentDir}.ToImmutableList(), null)
                },
                new FileOption
                {
                    Title = "(git) Push to origin",
                    ActionName = ScfeActions.GitPush,
                    CanActionBeApplied = (list, o) => IsGitRepository(new[] {_app.CurrentDir}.ToImmutableList(), null)
                },
                new FileOption
                {
                    Title = "(git) Pull from origin",
                    ActionName = ScfeActions.GitPull,
                    CanActionBeApplied = (list, o) => IsGitRepository(new[] {_app.CurrentDir}.ToImmutableList(), null)
                },
                new FileOption
                {
                    Title = "(git) Clone repository here",
                    ActionName = ScfeActions.GitClone,
                    CanActionBeApplied = (list, o) => !IsGitRepository(new[] {_app.CurrentDir}.ToImmutableList(), null)
                }
            };
        }

        public IEnumerable<FileOption> GetFilesOptions()
        {
            return new[]
            {
                new FileOption
                {
                    Title = "Stage file (if possible)",
                    ActionName = ScfeActions.GitStage,
                    CanActionBeApplied = IsGitRepository
                },
                new FileOption
                {
                    Title = "Unstage file (if possible)",
                    ActionName = ScfeActions.GitUnstage,
                    CanActionBeApplied = IsGitRepository
                }
            };
        }

        private bool IsGitRepository(ImmutableList<File> files, object arg2)
        {
            return files.Select(file => file.IsFolder() ? file : file.GetParent()).Distinct()
                .All(folder => Repository.Discover(folder.FullPath) != null);
        }

        private IEnumerable<string> DirStatusToString(DirectoryStatus status)
        {
            switch (status)
            {
                case DirectoryStatus.Ignored:
                    return new[] {"x", "ignored"};
                case DirectoryStatus.Unchanged:
                    return new[] {"=", "no changes"};
                case DirectoryStatus.ChangedInWorkDir:
                    return new[] {"*", "modified"};
                case DirectoryStatus.ChangedInIndex:
                    return new[] {"+", "staged"};
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private ConsoleColor? DirStatusToColor(DirectoryStatus status)
        {
            switch (status)
            {
                case DirectoryStatus.Ignored:
                    return ConsoleColor.DarkGray;
                case DirectoryStatus.Unchanged:
                    return null;
                case DirectoryStatus.ChangedInWorkDir:
                    return ConsoleColor.DarkYellow;
                case DirectoryStatus.ChangedInIndex:
                    return ConsoleColor.Green;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private void ComputeStatusAndCache(File file, string relPath)
        {
            var allFiles = file.GetAllChildren();
            foreach (var f in allFiles)
            {
                var stat = CachedFileStatus(f);
                if (WorkDirChangeStatuses.Any(x => (x & stat) == x))
                {
                    _cachedDirStatus.Add(relPath, DirectoryStatus.ChangedInWorkDir);
                    return;
                }

                if (IndexChangeStatuses.Any(x => (x & stat) == x))
                {
                    _cachedDirStatus.Add(relPath, DirectoryStatus.ChangedInIndex);
                    return;
                }
            }

            _cachedDirStatus.Add(relPath, DirectoryStatus.Unchanged);
        }

        private FileStatus CachedFileStatus(File file)
        {
            return CachedFileStatus(GetPathRelativeToRepo(file));
        }

        private IEnumerable<string> StatusToString(FileStatus retrieveStatus)
        {
            if (Corresponds(retrieveStatus, FileStatus.Nonexistent))
                return new[]
                {
                    "n", "not found"
                };
            if (Corresponds(retrieveStatus, FileStatus.NewInWorkdir))
                return new[]
                {
                    "+?", "new"
                };
            if (Corresponds(retrieveStatus, FileStatus.ModifiedInWorkdir))
                return new[]
                {
                    "*?", "modified"
                };
            if (Corresponds(retrieveStatus, FileStatus.DeletedFromWorkdir))
                return new[]
                {
                    "-?", "removed"
                };
            if (Corresponds(retrieveStatus, FileStatus.TypeChangeInWorkdir))
                return new[]
                {
                    "~?", "type change"
                };
            if (Corresponds(retrieveStatus, FileStatus.RenamedInWorkdir))
                return new[]
                {
                    "r?", "renamed"
                };
            if (Corresponds(retrieveStatus, FileStatus.NewInIndex))
                return new[]
                {
                    "++", "new added"
                };
            if (Corresponds(retrieveStatus, FileStatus.ModifiedInIndex))
                return new[]
                {
                    "*+", "mod added"
                };
            if (Corresponds(retrieveStatus, FileStatus.DeletedFromIndex))
                return new[]
                {
                    "-+", "rm added"
                };
            if (Corresponds(retrieveStatus, FileStatus.RenamedInIndex))
                return new[]
                {
                    "r+", "mv added"
                };
            if (Corresponds(retrieveStatus, FileStatus.TypeChangeInIndex))
                return new[]
                {
                    "~+", "typch added"
                };
            if (Corresponds(retrieveStatus, FileStatus.Unreadable))
                return new[]
                {
                    "N/A", "unreadable"
                };
            if (Corresponds(retrieveStatus, FileStatus.Ignored))
                return new[]
                {
                    "x", "ignored"
                };
            if (Corresponds(retrieveStatus, FileStatus.Conflicted))
                return new[]
                {
                    "!", "conflict"
                };
            if (Corresponds(retrieveStatus, FileStatus.Unaltered))
                return new[]
                {
                    "=", "no changes"
                };
            return new[]
            {
                "???"
            };
        }


        private bool Corresponds(FileStatus retrieveStatus, FileStatus toCheckAgainst)
        {
            return (retrieveStatus & toCheckAgainst) == toCheckAgainst;
        }

        private void RefreshRepository(File folder)
        {
            if (folder == null)
            {
                _cachedRepo?.Dispose();
                _cachedRepo = null;
                _cachedRepoPath = null;
                _repoStatus = null;
                _cachedDirStatus.Clear();
                _cachedIgnoredStatus.Clear();
                return;
            }

            var probeResult = Repository.Discover(folder.FullPath);
            if (probeResult != _cachedRepoPath)
            {
                RefreshRepository(null); // Reset the caches
                _cachedRepo = probeResult != null ? new Repository(probeResult) : null;
                _cachedRepoPath = probeResult;
                _repoStatus = _cachedRepo?.RetrieveStatus();
            }
        }

        public override ConsoleColor? GetColorForFile(File file)
        {
            if (_cachedRepo == null)
                return new DiscriminateDirectoriesAndHiddenScheme().GetColorForFile(file);
            if (file == null)
                return null;
            var relPath = GetPathRelativeToRepo(file);

            var status = CachedFileStatus(relPath);
            if (status == 0 && file.IsFolder())
            {
                if (!_cachedDirStatus.ContainsKey(relPath)) ComputeStatusAndCache(file, relPath);

                var st = _cachedDirStatus[relPath];
                if (st != DirectoryStatus.Unchanged)
                    return DirStatusToColor(st);
                return ConsoleColor.Cyan;
            }

            return StatusToColor(status);
        }

        private FileStatus CachedFileStatus(string relPath)
        {
            // Get the statuses that correspond to our file
            var en = _repoStatus.Where(se => se.FilePath == relPath);

            // And combine all of the known states into one using the OR bit-wise operator since the states
            // are just bit masks
            var result = en.Aggregate(FileStatus.Unaltered, (status, entry) => status | entry.State);
            if (!_cachedIgnoredStatus.ContainsKey(relPath))
                _cachedIgnoredStatus.Add(relPath, _cachedRepo.Ignore.IsPathIgnored(relPath));
            if (_cachedIgnoredStatus[relPath])
                result |= FileStatus.Ignored;
            return result;
        }

        private ConsoleColor? StatusToColor(FileStatus retrieveStatus)
        {
            if (Corresponds(retrieveStatus, FileStatus.NewInWorkdir))
                return ConsoleColor.DarkYellow;
            if (Corresponds(retrieveStatus, FileStatus.ModifiedInWorkdir))
                return ConsoleColor.DarkYellow;
            if (Corresponds(retrieveStatus, FileStatus.DeletedFromWorkdir))
                return ConsoleColor.DarkYellow;
            if (Corresponds(retrieveStatus, FileStatus.TypeChangeInWorkdir))
                return ConsoleColor.DarkYellow;
            if (Corresponds(retrieveStatus, FileStatus.RenamedInWorkdir))
                return ConsoleColor.DarkYellow;
            if (Corresponds(retrieveStatus, FileStatus.NewInIndex))
                return ConsoleColor.Green;
            if (Corresponds(retrieveStatus, FileStatus.ModifiedInIndex))
                return ConsoleColor.Green;
            if (Corresponds(retrieveStatus, FileStatus.DeletedFromIndex))
                return ConsoleColor.Green;
            if (Corresponds(retrieveStatus, FileStatus.RenamedInIndex))
                return ConsoleColor.Green;
            if (Corresponds(retrieveStatus, FileStatus.TypeChangeInIndex))
                return ConsoleColor.Green;
            if (Corresponds(retrieveStatus, FileStatus.Unreadable))
                return ConsoleColor.DarkRed;
            if (Corresponds(retrieveStatus, FileStatus.Ignored))
                return ConsoleColor.DarkGray;
            if (Corresponds(retrieveStatus, FileStatus.Conflicted))
                return ConsoleColor.Red;
            return null;
        }

        private string GetPathRelativeToRepo(File f)
        {
            if (_cachedRepo == null)
                return null;

            // We *need* a file representation WITHOUT a / (or backslash) at the end
            var repoFile = new File(_cachedRepoPath.EndsWith(Path.DirectorySeparatorChar)
                ? _cachedRepoPath.Substring(0, _cachedRepoPath.Length - 1)
                : _cachedRepoPath);
            if (repoFile.GetFileName() == ".git")
                repoFile = repoFile.GetParent();

            var relPath = f.GetRelativePath(repoFile).Replace(Path.DirectorySeparatorChar, '/');
            if (f.IsFolder() && !relPath.EndsWith("/"))
                relPath += "/";
            return relPath;
        }

        private enum DirectoryStatus
        {
            Ignored = -1,
            Unchanged = 0,
            ChangedInWorkDir = 1,
            ChangedInIndex = 1 << 1
        }

        ~GitExtension()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedRepo?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
