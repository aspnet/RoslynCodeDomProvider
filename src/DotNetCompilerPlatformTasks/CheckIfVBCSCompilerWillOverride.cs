using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetCompilerPlatformTasks
{
    public class CheckIfVBCSCompilerWillOverride : Task
    {
        [Required]
        public string Src { get; set; }
        [Required]
        public string Dest { get; set; }

        [Output]
        public bool WillOverride { get; set; }

        public override bool Execute()
        {
            WillOverride = false;

            try
            {
                WillOverride = File.Exists(Src) && File.Exists(Dest) && (File.GetLastWriteTime(Src) != File.GetLastWriteTime(Dest));
            }
            catch { return false; }

            return true;
        }
    }
}
