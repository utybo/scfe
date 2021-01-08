/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;

namespace SCFE
{
    public abstract class FileColorScheme
    {
        public abstract ConsoleColor? GetColorForFile(File file);
    }

    public class DiscriminateDirectoriesAndHiddenScheme : FileColorScheme
    {
        public override ConsoleColor? GetColorForFile(File file)
        {
            if (file.IsHidden()) return file.IsFolder() ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
            return file.IsFolder() ? ConsoleColor.Green : (ConsoleColor?) null;
        }
    }
}
