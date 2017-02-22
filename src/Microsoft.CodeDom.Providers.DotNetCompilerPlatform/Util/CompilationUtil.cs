// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

﻿using System.CodeDom.Compiler;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    internal static class CompilationUtil {
        internal static void PrependCompilerOption(CompilerParameters compilParams, string compilerOptions) {
            if (compilParams.CompilerOptions == null) {
                compilParams.CompilerOptions = compilerOptions;
            }
            else {
                compilParams.CompilerOptions = compilerOptions + " " + compilParams.CompilerOptions;
            }
        }
    }
}
