using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetCompilerPlatformTasks
{
    public class KillProcess : Task
    {
        [Required]
        public string ProcessName { get; set; }
        [Required]
        public string ImagePath { get; set; }

        public override bool Execute()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(ProcessName))
                {
                    var wmiQuery = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + p.Id;
                    using (var searcher = new ManagementObjectSearcher(wmiQuery))
                    {
                        using (var results = searcher.Get())
                        {
                            var mo = results.Cast<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                var path = (string)mo["ExecutablePath"];
                                var executablePath = path ?? string.Empty;
                                Log.LogMessage("ExecutablePath is {0}", executablePath);

                                if (executablePath.StartsWith(ImagePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    p.Kill();
                                    p.WaitForExit();
                                    Log.LogMessage("{0} is killed", executablePath);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning(ex.Message);
            }
            return true;
        }
    }
}
