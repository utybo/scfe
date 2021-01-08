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
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;
using Viu;
using Viu.Table;

namespace SCFE
{
    public class ComExtension : IScfeExtension
    {
        private ScfeApp _app;
        private Dictionary<int, Process> _managedProcesses = new Dictionary<int, Process>();

        public ComExtension(ScfeApp app)
        {
            _app = app;
        }

        public IEnumerable<ColumnType<File>> GetColumns()
        {
            // No columns for the com mode
            return new List<ColumnType<File>>();
        }

        public Dictionary<string, Action<object, ActionEventArgs>> GetActions()
        {
            return new Dictionary<string, Action<object, ActionEventArgs>>
            {
                {
                    ScfeActions.ComMode, (o, args) =>
                    {
                        _app.PrintModeInformation(NavigationMode.ComMode, args.Graphics);
                        _app.Request("Command...", args.Graphics,
                            (s, context, finish) =>
                            {
                                finish();
                                Regex reg = new Regex(@"^(\d+)\$(.+)$");
                                Match match = reg.Match(s);
                                if (match.Success)
                                {
                                    int id = int.Parse(match.Groups[1].Value);
                                    if (!_managedProcesses.ContainsKey(id))
                                    {
                                        _app.ShowHelpMessage("Invalid or untracked PID: " + id, args.Graphics);
                                        return;
                                    }

                                    _managedProcesses[id].StandardInput.WriteLine(s);
                                    return;
                                }

                                _app.PrintModeInformation(_app.CurrentMode, args.Graphics);
                                _app.AddTask(task =>
                                {
                                    try
                                    {
                                        string filename;
                                        string pargs;
                                        switch (Environment.OSVersion.Platform)
                                        {
                                            case PlatformID.Win32NT:
                                                filename = "powershell";
                                                pargs = "-NonInteractive -Command \"& {" +
                                                        s.Trim().Replace("\"", "\"\"\"") + "}\"";
                                                break;
                                            case PlatformID.Unix:
                                            case PlatformID.MacOSX:
                                                filename = "bash";
                                                pargs = "-c \"" + s.Replace("\"", "\\\"") + "\"";
                                                break;
                                            default:
                                                return new TaskResult(false,
                                                    "COM mode is not supported on your platform");
                                        }

                                        var proc = new Process
                                        {
                                            StartInfo = new ProcessStartInfo
                                            {
                                                FileName = filename,
                                                Arguments = pargs,
                                                UseShellExecute = false,
                                                RedirectStandardInput = true,
                                                RedirectStandardOutput = true,
                                                RedirectStandardError = true,
                                                WorkingDirectory = _app.CurrentDir.FullPath
                                            }
                                        };
                                        proc.Start();
                                        Thread.Sleep(100);
                                        _managedProcesses.Add(proc.Id, proc);
                                        _app.ShowHelpMessageLater("Command started executing with PID " + proc.Id);

                                        string lastLine = null;
                                        while (!proc.StandardOutput.EndOfStream)
                                        {
                                            var line = proc.StandardOutput.ReadLine();
                                            if (line != null)
                                            {
                                                line = line.Replace("\n", "").Replace("\t", "");
                                                if (!string.IsNullOrWhiteSpace(line))
                                                {
                                                    lastLine = line;
                                                    _app.ShowHelpMessageLater($"[{proc.Id}] {line}");
                                                }
                                            }
                                        }

                                        if (proc.ExitCode == 0)
                                            return new TaskResult(true,
                                                lastLine != null
                                                    ? $"[{proc.Id}] Finished: {lastLine}"
                                                    : $"[{proc.Id}] Process finished");

                                        string errorMsg = proc.StandardError.ReadLine();
                                        if (errorMsg == null)
                                            return new TaskResult(false,
                                                $"[{proc.Id}] Failed (exit code {proc.ExitCode})");
                                        return new TaskResult(false,
                                            $"[{proc.Id}] Failed (code {proc.ExitCode}): {errorMsg}");
                                    }
                                    catch (Exception e)
                                    {
                                        return new TaskResult(false, "Error: " + e.Message);
                                    }
                                });
                            }, false, g => { _app.PrintModeInformation(_app.CurrentMode, g); });
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
                    Title = "Switch to COM mode",
                    ActionName = ScfeActions.ComMode
                }
            };
        }

        public IEnumerable<FileOption> GetFilesOptions()
        {
            return new List<FileOption>();
        }
    }
}
