// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    public sealed class ProviderOptions : IProviderOptions {

        private IDictionary<string, string> _allOptions;

        public ProviderOptions()
        {
            this.CompilerFullPath = null;
            this.CompilerVersion = null;
            this.WarnAsError = false;
            this.AllOptions = null;

            // This results in no keep-alive for the compiler. This will likely result in
            // slower performance for any program that calls out the the compiler any
            // significant number of times. This is why the CompilerUtil.GetProviderOptionsFor
            // does not leave this as 0.
            this.CompilerServerTimeToLive = 0;

            // This is different from the default that the CompilerUtil.GetProviderOptionsFor
            // factory method uses. The primary known user of the factory method is us, and
            // this package is first intended to support ASP.Net. However, if somebody is
            // creating an instance of this directly, they are probably not an ASP.Net
            // project. Thus the different default here.
            this.UseAspNetSettings = false; 
        }

        public ProviderOptions(IProviderOptions opts) : this()
        {
            this.CompilerFullPath = opts.CompilerFullPath;
            this.CompilerServerTimeToLive = opts.CompilerServerTimeToLive;
            this.CompilerVersion = opts.CompilerVersion;
            this.WarnAsError = opts.WarnAsError;
            this.UseAspNetSettings = opts.UseAspNetSettings;
            this.AllOptions = opts.AllOptions;
        }

        internal ProviderOptions(ICompilerSettings settings) : this()
        {
            this.CompilerFullPath = settings.CompilerFullPath;
            this.CompilerServerTimeToLive = settings.CompilerServerTimeToLive;
        }

        public string CompilerFullPath { get; internal set; }

        public int CompilerServerTimeToLive { get; internal set; }

        // smolloy todo debug degub - we don't use this. It is used by the framework. Do we care to call it out like this?
        public string CompilerVersion { get; internal set; }

        public bool WarnAsError { get; internal set; }

        public bool UseAspNetSettings { get; internal set; }

        public IDictionary<string, string> AllOptions {
            get {
                return _allOptions;
            }
            internal set {
                _allOptions = new ReadOnlyDictionary<string, string>(value);
            }
        }
    }
}
