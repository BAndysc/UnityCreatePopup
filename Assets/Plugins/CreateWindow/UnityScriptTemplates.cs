using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CreateWindow
{
    internal static class UnityScriptTemplates
    {
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

            NewBehaviourScriptPath = scriptTemplatesDir + "81-C# Script-NewBehaviourScript.cs.txt";
            StandardSurfaceShader =
                scriptTemplatesDir + "83-Shader__Standard Surface Shader-NewSurfaceShader.shader.txt";
            UnlitShader = scriptTemplatesDir + "84-Shader__Unlit Shader-NewUnlitShader.shader.txt";
            ImageEffectShader = scriptTemplatesDir + "85-Shader__Image Effect Shader-NewImageEffectShader.shader.txt";
            ComputeShader = scriptTemplatesDir + "90-Shader__Compute Shader-NewComputeShader.compute.txt";
            
            PlayableBehaviour = scriptTemplatesDir + "87-Playables__Playable Behaviour C# Script-NewPlayableBehaviour.cs.txt";
            PlayableAsset = scriptTemplatesDir + "88-Playables__Playable Asset C# Script -NewPlayableAsset.cs.txt";
            
            AssemblyDefinition = scriptTemplatesDir + "91-Assembly Definition-NewAssembly.asmdef.txt";
            AssemblyDefinitionReference = scriptTemplatesDir + "93-Assembly Definition Reference-NewAssemblyReference.asmref.txt";
        }

        public static String NewBehaviourScriptPath;
        public static String StandardSurfaceShader;
        public static String UnlitShader;
        public static String ImageEffectShader;
        public static String ComputeShader;
        
        public static String PlayableBehaviour;
        public static String PlayableAsset;
        
        public static String AssemblyDefinition;
        public static String AssemblyDefinitionReference;
    }
}