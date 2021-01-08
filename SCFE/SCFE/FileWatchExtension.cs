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
using System.IO;
using System.Linq;
using Viu;
using Viu.Table;

namespace SCFE
{
    public class FileWatchExtension : IScfeExtension
    {
        private ScfeApp _app;
        private FileSystemWatcher _watcher;

        public FileWatchExtension(ScfeApp app)
        {
            _app = app;
            _app.OnFolderViewChanged += OnAppFolderViewChanged;
        }

        public static List<File> TempIgnoreList { get; } = new List<File>();

        private void OnAppFolderViewChanged(object sender, FolderViewChangedEventArgs args)
        {
            if (args.FolderChanged)
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                }

                _watcher = new FileSystemWatcher
                {
                    Path = args.NewFolder.FullPath,
                    IncludeSubdirectories = false
                };
                _watcher.Changed += RefreshAppDirectory;
                _watcher.Deleted += RefreshAppDirectory;
                _watcher.Renamed += RefreshAppDirectory;
                _watcher.Created += RefreshAppDirectory;
                _watcher.EnableRaisingEvents = true;
            }
        }

        private void RefreshAppDirectory(object sender, FileSystemEventArgs e)
        {
            File f;
            if ((f = TempIgnoreList.FirstOrDefault(fi => fi.FullPath.Equals(e.FullPath))) != null)
            {
                TempIgnoreList.Remove(f);
                return;
            }
            var cachedCurrentDir = _app.CurrentDir;
            _app.DoGraphicsLater(g =>
            {
                if (cachedCurrentDir == _app.CurrentDir)
                    _app.SwitchToFolder(_app.CurrentDir, g, _app.FocusedElement);
            });
        }

        public IEnumerable<ColumnType<File>> GetColumns()
        {
            return new List<ColumnType<File>>();
        }

        public Dictionary<string, Action<object, ActionEventArgs>> GetActions()
        {
            return new Dictionary<string, Action<object, ActionEventArgs>>();
        }

        public IEnumerable<FileOption> GetCurrDirOptions()
        {
            return new List<FileOption>();
        }

        public IEnumerable<FileOption> GetFilesOptions()
        {
            return new List<FileOption>();
        }
    }
}
