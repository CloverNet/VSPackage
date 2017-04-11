using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommandBuild
{
    public class DevenvInstance
    {
        public ReadOnlyDictionary<string, string> DevenvFileNames;
        public string CurrentDevenvFullName { get; private set; }
        internal bool FilterCommand = false;

        public event EventHandler Starting;
        public event DataReceivedEventHandler OutputString;
        public event EventHandler Exited;

        public DevenvInstance()
        {
            DevenvFileNames = new ReadOnlyDictionary<string, string>(GetDevenvFileNames());
            if (DevenvFileNames.Any())
            {
                CurrentDevenvFullName = DevenvFileNames.First().Value;
            }
        }

        internal Dictionary<string, string> GetDevenvFileNames()
        {
            var vsRoot = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio");
            return vsRoot.GetSubKeyNames()
                .Where(m => Regex.IsMatch(m, @"\d+\.\d+_Config"))
                .ToDictionary(
                    m => m.Replace("_Config", ""),
                    m => vsRoot.OpenSubKey(m).GetValue("InstallDir").ToString() + "devenv.exe");
        }

        public void SetCurrentDevenvFullName(string key)
        {
            if (DevenvFileNames.ContainsKey(key))
            {
                CurrentDevenvFullName = DevenvFileNames[key];
            }
            else if (System.IO.File.Exists(key))
            {
                CurrentDevenvFullName = key;
            }
            else
            {
                throw new KeyNotFoundException(key);
            }

        }

        public void Open(string slnFile)
        {
            var command = @"devenv {0}";
            command = string.Format(command, slnFile);
            Exec(command);
        }

        public void Build(string slnFile)
        {
            Build(slnFile, "Debug|Any CPU");
        }

        public void Build(string slnFile, string slnConfig)
        {
            var command = @"devenv {0} /build ""{1}""";
            command = string.Format(command, slnFile, slnConfig);
            Exec(command);
        }

        public void BuildProject(string slnFile, string slnConfig, string csProject, string projConfig)
        {
            var command = @"devenv {0} /project {1} /projectConfig ""{2}"" /build ""{3}"" ";
            command = string.Format(command, slnFile, csProject, projConfig, slnConfig);
            Exec(command);
        }

        public void ReBuild(string slnFile)
        {
            ReBuild(slnFile, "Debug|Any CPU");
        }

        public void ReBuild(string slnFile, string slnConfig)
        {
            var command = @"devenv {0} /rebuild ""{1}"" ";

            command = string.Format(command, slnFile, slnConfig);
            Exec(command);
        }

        public void ReBuildProject(string slnFile, string slnConfig, string csProject, string projConfig)
        {
            var command = @"devenv {0} /project {1} /projectConfig ""{2}"" /rebuild ""{3}"" ";
            command = string.Format(command, slnFile, csProject, projConfig, slnConfig);
            Exec(command);
        }

        public void Clean(string slnFile)
        {
            var command = @"devenv {0} /clean ";
            command = string.Format(command, slnFile);
            Exec(command);
        }

        public void CleanProject(string slnFile, string csProject)
        {
            var command = @"devenv {0} /project {1} /clean ";
            command = string.Format(command, slnFile, csProject);
            Exec(command);
        }

        public virtual void Exec(string command)
        {
            var commandOut = false;

            using (var process = new Process()
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();

                if (null != Starting) Starting(process, new EventArgs());

                process.StandardInput.WriteLine("cd /d " + Path.GetDirectoryName(CurrentDevenvFullName));
                process.StandardInput.WriteLine(command);
                process.StandardInput.WriteLine("exit");

                process.OutputDataReceived += (sender, e) =>
                {
                    if (commandOut)
                    {
                        if (e.Data != null)
                        {
                            if (e.Data.EndsWith("exit"))
                            {
                                if (null != Exited) Exited(sender, e);
                            }
                            else if (null != OutputString)
                            {
                                OutputString(sender, e);
                            }
                        }
                    }
                    else if (e.Data != null && e.Data.Contains(command))
                    {
                        commandOut = true;
                    }
                };

                process.BeginOutputReadLine();
            }
        }

    }
}