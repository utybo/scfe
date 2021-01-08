/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using JetBrains.Annotations;
using Viu;

namespace SCFE
{
    public class FolderViewChangedEventArgs
    {
        public bool FolderChanged { get; }
        
        [CanBeNull]
        public File OldFolder { get; }
        
        [NotNull]
        public File NewFolder { get; }
        
        [CanBeNull]
        public GraphicsContext Graphics { get; }

        public FolderViewChangedEventArgs(bool folderChanged, File oldFolder, File newFolder, GraphicsContext graphics)
        {
            FolderChanged = folderChanged;
            OldFolder = oldFolder;
            NewFolder = newFolder;
            Graphics = graphics;
        }
    }
}
