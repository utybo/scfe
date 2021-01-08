/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.IO;

namespace SCFE
{
    public class FileException : IOException
    {
        public FileException()
        {
        }

        public FileException(string message) : base(message)
        {
        }

        public FileException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
