using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CreateWindow
{
    internal static class UnityScriptTemplates
    {
        // they are somehow ignored normally
        private static string[] ignoredTemplates =
        {
            "92-Assembly Definition-NewEditModeTestAssembly.asmdef", "92-Assembly Definition-NewTestAssembly.asmdef", // 2018.1 +
            "83-C# Script-NewTestScript.cs", // 2018.1 +
            
            "86-C# Script-NewStateMachineBehaviourScript.cs", "86-C# Script-NewSubStateMachineBehaviourScript.cs", // 2017 + 
        };
        
        static UnityScriptTemplates()
        {
            var entryAssembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
            var managedDir = Path.GetDirectoryName(entryAssembly.Location);
            var scriptTemplatesDir = managedDir + "/../Resources/ScriptTemplates/";

            if (!Directory.Exists(scriptTemplatesDir))
            {
                scriptTemplatesDir = managedDir + "/../../Resources/ScriptTemplates/";
            }
            if (!Directory.Exists(scriptTemplatesDir))
            {
                UnityEngine.Debug.LogError("Can't find directory: " + scriptTemplatesDir);
                return;
            }

            string [] fileEntries = Directory.GetFiles(scriptTemplatesDir);
            Entries = new List<Entry>();
            foreach (string fullPath in fileEntries)
            {
                var fileName = Path.GetFileNameWithoutExtension(fullPath);
                
                if (ignoredTemplates.Contains(fileName))
                    continue;
                var parts = fileName.Split('-');
                Debug.Assert(parts.Length == 3, "Unexpected path: " + fullPath);

                var index = Convert.ToInt32(parts[0]);
                var path = parts[1].Replace("__", "/");
                var generatedFileName = parts[2];
                
                Entries.Add(new Entry(index, path.Trim(), generatedFileName, fullPath));
            }
        }

        internal static readonly List<Entry> Entries;
        
        public class Entry
        {
            public readonly int Priority;
            public readonly string Path;
            public readonly string FileName;
            public readonly string TemplatePath;

            public Entry(int priority, string path, string fileName, string templatePath)
            {
                Priority = priority;
                Path = path;
                FileName = fileName;
                TemplatePath = templatePath;
            }
        }
    }
}