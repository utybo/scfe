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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace SCFE
{
    public class File
    {
        /// <summary>
        ///     The path of the file represented by this File object.
        ///     This should ***NEVER*** have a trailing slash or backslash
        /// </summary>
        private readonly string _path;

        public File(string path)
        {
            _path = path;
        }

        public string Path => _path;

        public string FullPath => System.IO.Path.GetFullPath(Path);

        public static File CurrentDirectory
        {
            get => new File(Directory.GetCurrentDirectory());
            set
            {
                Directory.SetCurrentDirectory(value._path);
                Environment.CurrentDirectory = value._path;
            }
        }

        public static File UserHome =>
            new File(Environment.OSVersion.Platform == PlatformID.Unix ||
                     Environment.OSVersion.Platform == PlatformID.MacOSX
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"));

        public bool Exists()
        {
            // Done
            // Should return whether the file exists or not

            return System.IO.File.Exists(_path) || Directory.Exists(_path);
        }

        public void CreateFile(bool createDirectories = false)
        {
            if (Exists())
                throw new FileException("The file already exists, please modify the name of your new file.");

            var parent = GetParent();
            if (parent != null && !parent.Exists())
            {
                if (createDirectories)
                    parent.CreateDirectory(true);
                else
                    throw new FileException("Error: The parent folder does not exist");
            }

            using (System.IO.File.Create(_path))
            {
            }
        }

        public bool IsFolder()
        {
            //Done
            //Returns true if the file exists and is a Folder
            //Returns false in any other case

            return Directory.Exists(_path);
        }

        [CanBeNull]
        public File GetParent()
        {
            // Done
            // Should return the parent directory of this file
            // e.g. Path = my/folder/text.txt, if this instance represents "text.txt", this should return the File that corresponds to
            // "folder"
            var par = Directory.GetParent(_path.TrimEnd('\\'));
            if (par != null)
                return new File(par.FullName);
            return null;
        }

        public void CreateDirectory(bool createParents = false)
        {
            // Done
            // Should create the directory that corresponds to this file
            // e.g. Path = my/folder/, if this instance represents "folder", create the folder.
            // If the folder already exists, do nothing
            // If the folder already exists, but is a file, throw a FileException
            // If the folder does not exist but its parent does not exist either (e.g. "my" is also missing), either
            // create all of the parent directories if createParents is true, or throw a FileException if createParents
            // is false.

            if (Directory.Exists(_path))
                return;

            if (Exists())
                throw new FileException("The directory already exists, but points to a file.");

            if (!GetParent().IsFolder() && !createParents)
                throw new FileException("The parent directory does not exist and could not be created");

            Directory.CreateDirectory(_path);
        }

        public void CopyTo([NotNull] File destination, bool overwrite = false)
        {
            // Done, but needs the 'IsFolder() case' needs to be improved

            // Should copy the contents of the current file to the destination file.
            // If the destination file does not exist, create it (but do not create the parent folders if they do not
            // exist)
            // If the destination file already exists but is a folder, throw a FileException
            // If the destination file already exists and is a file, either overwrite it if overwrite is true, or throw 
            // a FileException if overwrite is false.

            if (IsFolder())
            {
                // TODO
                if (destination.Exists())
                    throw new FileException("The destination file already exists.");

                destination.CreateDirectory();

                var fileInCurrentFolder = GetChildren();
                foreach (var file in fileInCurrentFolder)
                    file.CopyTo(destination.GetChild(file.GetFileName()), overwrite);

                // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
            }
            else
            {
                if (destination.IsFolder())
                    throw new FileException("Destination file is a folder");
                if (destination.Exists() && !overwrite)
                    throw new FileException("The file already exists and could not be overwritten.");
                if (destination.Exists())
                    System.IO.File.Delete(destination._path);

                System.IO.File.Copy(_path, destination._path);
            }
        }

        public void MoveTo([NotNull] File destination, bool overwrite = false)
        {
            // Done, but needs the 'IsFolder() case' needs to be improved

            // Should move the contents of the current file to the destination file.
            // If the destination file does not exist, create it (but do not create the parent folders if they do not
            // exist)
            // If the destination file already exists but is a folder, throw a FileException
            // If the destination file already exists and is a file, either overwrite it if overwrite is true, or throw 
            // a FileException if overwrite is false.

            if (IsFolder())
            {
                // Done
                if (destination.Exists())
                    throw new FileException("The destination file already exists.");

                Directory.Move(_path, destination._path);

                // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
            }
            else
            {
                if (destination.IsFolder())
                    throw new FileException("Destination file is a folder");

                if (destination.Exists() && !overwrite)
                    throw new FileException("The file already exists and could not be overwritten.");

                if (destination.Exists())
                    System.IO.File.Delete(destination._path);

                System.IO.File.Move(_path, destination._path);
            }
        }

        [NotNull]
        public File[] GetChildren()
        {
            // Done
            // Should get the children files of the current folder represented by this file object. All of the files
            // in the returned array MUST exist
            // If the current folder does not exist, throw a FileException
            // If this object represents a folder, throw a FileException

            if (!Directory.Exists(_path))
                throw new FileException("The current folder does not exist");

            if (!IsFolder())
                throw new FileException("The current directory is not a folder");

            var listOfFileNames = Directory.GetFiles(_path);
            var listOfDirectoryNames = Directory.GetDirectories(_path);
            var listOfFiles = listOfFileNames.Select(t => new File(t)).ToList();
            listOfFiles.AddRange(listOfDirectoryNames.Select(t => new File(t)));
            return listOfFiles.OrderBy(t => t._path).ToArray();
        }

        [NotNull]
        public File GetChild([NotNull] string filename)
        {
            // Done
            // Should get a child of the current folder. The resulting file may or may not exist.
            // If the current folder does not exist or is not a folder, throw a FileException


            if (!Exists())
                throw new FileException("The current directory does not exist");

            if (!IsFolder())
                throw new FileException("The current directory is not folder");

            return new File(_path + System.IO.Path.DirectorySeparatorChar + filename);
        }

        [CanBeNull]
        public File GetChildMaybe([NotNull] string filename)
        {
            if (!Exists() || !IsFolder())
                return null;

            return new File(_path + System.IO.Path.DirectorySeparatorChar + filename);
        }

        public string GetFileName()
        {
            //Done
            // returns the file name if path represents a file
            // returns a empty string otherwise

            return System.IO.Path.GetFileName(_path);
        }

        [NotNull]
        public File GetSibling(string filename)
        {
            // Done
            // Should get a file that is in the same folder as this one. e.g.
            // my/folder/text.txt
            // my/folder/amazing.txt
            // Calling
            //     new File("my/folder/text.txt").GetSibling("amazing.txt")
            // should return an object that represents "my/folder/amazing.txt"

            return GetParent().GetChild(filename);
        }

        private static string BytesToString(long byteCount, int maxLen)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), maxLen - 4);
            return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
        }

        public string GetSizeString(int maxLen)
        {
            if (IsFolder())
                return maxLen >= 5 ? "<dir>" : "dir";
            return BytesToString(new FileInfo(_path).Length, maxLen);
        }

        public long GetSize()
        {
            return IsFolder() ? -1 : new FileInfo(_path).Length;
        }

        public DateTime GetModificationDate()
        {
            return System.IO.File.GetLastWriteTime(_path);

            //Done
            // Return the modification date of the file
        }

        public DateTime GetCreationDate()
        {
            return System.IO.File.GetCreationTime(_path);
            //Done
            // Return the creation date of the file
        }

        public void Open()
        {
            if (IsFolder())
                throw new FileException("Cannot open a folder: this method only opens files into the system's app");
            var proc = new Process {StartInfo = {FileName = _path, UseShellExecute = true}};
            proc.Start();
        }


        public bool IsHidden()
        {
            var fileInfo = new FileInfo(_path);
            return fileInfo.Attributes.HasFlag(FileAttributes.Hidden) || GetFileName().StartsWith(".");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((File) obj);
        }

        protected bool Equals(File other)
        {
            return string.Equals(FullPath, other.FullPath);
        }

        public override int GetHashCode()
        {
            return _path != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_path) : 0;
        }

        public static bool operator ==(File left, File right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(File left, File right)
        {
            return !Equals(left, right);
        }

        public void Delete(bool deleteIfFolder)
        {
            if (!Exists())
                return;
            if (IsFolder() && deleteIfFolder)
            {
                var di = new DirectoryInfo(_path);
                di.Delete(true);
            }
            else
            {
                System.IO.File.Delete(_path);
            }
        }

        public string GetRelativePath(File relativeTo)
        {
            var pathSep = System.IO.Path.DirectorySeparatorChar + "";
            var fromPath = FullPath;
            var
                baseDir = relativeTo.FullPath;
            // If folder contains upper folder references, they gets lost here. "c:\test\..\test2" => "c:\test2"

            var p1 = Regex.Split(fromPath, "[\\\\/]").Where(x => x.Length != 0).ToArray();
            var p2 = Regex.Split(baseDir, "[\\\\/]").Where(x => x.Length != 0).ToArray();
            var i = 0;

            for (; i < p1.Length && i < p2.Length; i++)
                if (string.Compare(p1[i], p2[i], StringComparison.OrdinalIgnoreCase) != 0)
                    // Case insensitive match
                    break;

            if (i == 0)
                // Cannot make relative path, for example if resides on different drive
                return null;

            var r = string.Join(pathSep,
                Enumerable.Repeat("..", p2.Length - i).Concat(p1.Skip(i).Take(p1.Length - i)));
            return r;
        }

        public IEnumerable<File> GetAllChildren()
        {
            return Directory.EnumerateFiles(FullPath, "*", SearchOption.AllDirectories).Select(s => new File(s));
        }
    }
}
