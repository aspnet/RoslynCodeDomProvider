// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform {
    static class AppSettings {
        private static volatile bool _settingsInitialized;
        private static object _lock = new object();

        private static void LoadSettings(NameValueCollection appSettings) {
            string disableProfilingDuringCompilation = appSettings["aspnet:DisableProfilingDuringCompilation"];

            if (!bool.TryParse(disableProfilingDuringCompilation, out _disableProfilingDuringCompilation)) {
                _disableProfilingDuringCompilation = true;
            }
        }

        private static void EnsureSettingsLoaded() {
            if (_settingsInitialized) {
                return;
            }

            lock (_lock) {
                if (!_settingsInitialized) {
                    try {
                        LoadSettings(WebConfigurationManager.AppSettings);
                    }
                    finally {
                        _settingsInitialized = true;
                    }
                }
            }
        }

        private static bool _disableProfilingDuringCompilation = true;
        public static bool DisableProfilingDuringCompilation {
            get {
                EnsureSettingsLoaded();
                return _disableProfilingDuringCompilation;
            }
        }
    }
}
