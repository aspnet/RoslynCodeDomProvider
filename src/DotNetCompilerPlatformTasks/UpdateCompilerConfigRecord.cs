// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DotNetCompilerPlatformTasks
{
    public class UpdateCompilerConfigRecord : Task
    {
        [Required]
        public string ConfigFile { get; set; }
        [Required]
        public string Extension { get; set; }
        [Required]
        public string Language { get; set; }
        [Required]
        public string WarningLevel { get; set; }
        [Required]
        public string Options { get; set; }
        [Required]
        public string CompilerType { get; set; }

        private readonly string singleTab = "  ";


        public override bool Execute()
        {
            try
            {
                XmlDocument config = new XmlDocument() { PreserveWhitespace = true };
                config.Load(ConfigFile);

                // Look for existing 'compiler' record first. Keying on 'extension' is a little dubious since that attribute can
                // technically be a ';' separated list of extensions... but it's what we always seemed to do in the past.
                XmlNode compiler = config.SelectSingleNode($"/configuration/system.codedom/compilers/compiler[@extension='{Extension}']");

                // Create the 'compiler' record if not found.
                if (compiler == null)
                {
                    string indent = "";
                    XmlElement e = CreateElement(config, "system.codedom/compilers", "compiler", ref indent);
                    e.SetAttribute("language", Language);
                    e.SetAttribute("extension", Extension);
                    e.SetAttribute("warningLevel", WarningLevel);
                    e.SetAttribute("compilerOptions", Options);
                    e.SetAttribute("type", CompilerType);

                    // Add a 'providerOption' to not do ASP.Net "magic" within the codedom provider if not working on web.config
                    if (!ConfigFile.EndsWith("web.config", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        XmlElement pOpt = CreateIndentedElement(e, "providerOption", indent + singleTab);
                        pOpt.SetAttribute("name", "UseAspNetSettings");
                        pOpt.SetAttribute("value", "false");
                    }

                    config.Save(ConfigFile);
                    return true;
                }

                // Otherwise, leave the existing compiler alone - including any 'providerOptions' - except...
                // Ensure the 'type' value is current.
                var typeAttr = compiler.Attributes["type"];
                if (typeAttr == null || string.Compare(typeAttr.Value, CompilerType, System.StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    typeAttr = config.CreateAttribute("type");
                    typeAttr.Value = CompilerType;
                    compiler.Attributes.SetNamedItem(typeAttr);
                    config.Save(ConfigFile);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }

            return true;
        }

        private XmlElement CreateElement(XmlDocument doc, string path, string name, ref string indent)
        {
            XmlElement current = doc.DocumentElement;

            foreach (var partName in path.Trim('/').Split('/'))
            {
                if (string.IsNullOrWhiteSpace(partName))
                    continue;

                current = (current.SelectSingleNode(partName) as XmlElement) ?? CreateIndentedElement(current, partName, indent);
                indent += singleTab;
            }

            return CreateIndentedElement(current, name, indent);
        }

        private XmlElement CreateIndentedElement(XmlNode parent, string name, string indent)
        {
            if (!parent.HasChildNodes)
                parent.AppendChild(parent.OwnerDocument.CreateWhitespace(Environment.NewLine + indent + singleTab));
            else
                parent.AppendChild(parent.OwnerDocument.CreateWhitespace(singleTab));

            XmlElement e = parent.AppendChild(parent.OwnerDocument.CreateElement(name)) as XmlElement;
            parent.AppendChild(parent.OwnerDocument.CreateWhitespace(Environment.NewLine + indent));
            return e;
        }
    }
}
