// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System.Collections.Specialized;
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

            _roslynCompilerLocation =  appSettings["aspnet:RoslynCompilerLocation"];
        }

        private static void EnsureSettingsLoaded() {
            if (_settingsInitialized) {
                return;
            }

            lock (_lock) {
                if (!_settingsInitialized) {
                    try {
                        // I think it should be safe to straight up use regular ConfigurationManager here...
                        // but if it ain't broke, don't fix it.
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

        private static string _roslynCompilerLocation = string.Empty;
        public static string RoslynCompilerLocation {
            get {
                EnsureSettingsLoaded();
                return _roslynCompilerLocation;
            }
        }
    }
}
