using System;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal interface ICompilerSettings {
        string CompilerFullPath { get; }

        // TTL in seconds
        int CompilerServerTimeToLive { get; }
    }
}
