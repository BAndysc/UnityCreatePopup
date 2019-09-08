using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using Object = UnityEngine.Object;

namespace CreateWindow
{
    internal static class CreateItemsProvider
    {
        static CreateItemsProvider()
        {
            entries = new List<Entry>();
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var menuItems = new Dictionary<string, Entry.MenuItemDelegate>();
            var priority = new Dictionary<string, int>();
            var validators = new Dictionary<string, Entry.ValidatorDelegate>();
            
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<CreateAssetMenuAttribute>();
                    if (attr != null)
                    {
                        var name = attr.menuName;

                        if (string.IsNullOrEmpty(name))
                            name = type.Name;

                        string itemName;
                        var parent = EnsureByPath(name, attr.order, out itemName);

                        var entry = new Entry(itemName, () =>
                        {
                            var path = attr.fileName;
                            if (string.IsNullOrEmpty(path))
                                path = itemName + ".asset";

                            ProjectWindowUtil.CreateAsset(ScriptableObject.CreateInstance(type), path);
                        }, () => true, parent, attr.order, type);
                         
                        if (parent == null)
                            entries.Add(entry); 
                        else
                            parent.Children.Add(entry);
                    }

                    var staticMethods =
                        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    
                    menuItems.Clear();
                    validators.Clear();
                    priority.Clear();
                    
                    foreach (var method in staticMethods)
                    {
                        var menuItemAttr = method.GetCustomAttribute<MenuItem>();
                        if (menuItemAttr == null) 
                            continue;

                        if (!menuItemAttr.menuItem.Contains("Create/") || menuItemAttr.menuItem.Contains("internal:")) 
                            continue;
                        
                        if (menuItemAttr.validate)
                            validators.Add(menuItemAttr.menuItem, (Entry.ValidatorDelegate)method.CreateDelegate(typeof(Entry.ValidatorDelegate), null));
                        else
                        {
                            priority[menuItemAttr.menuItem] = menuItemAttr.priority;
                            menuItems.Add(menuItemAttr.menuItem, (Entry.MenuItemDelegate)method.CreateDelegate(typeof(Entry.MenuItemDelegate), null));
                        }
                    }

                    Entry.ValidatorDelegate validator;
                    foreach (var menuItem in menuItems.Keys)
                    {
                        var method = menuItems[menuItem];
                        validators.TryGetValue(menuItem, out validator);

                        if (validator == null)
                            validator = () => true;
                    
                        var name = menuItem;
                        name = name.Replace("internal:", "").Replace("Assets/Create/", "");

                        string itemName;
                        var parent = EnsureByPath(name, priority[menuItem], out itemName);

                        var entry = new Entry(itemName, method, validator, parent, priority[menuItem]);
                            
                        if (parent == null)
                            entries.Add(entry);
                        else
                            parent.Children.Add(entry);
                    }
                } 
            }

            AddInternals();
            
            entries.Sort(new EntryComparer());
        }

        delegate void CreateSpritePolygonType(int sides);
        
        /// <summary>
        /// Those items are added by Unity native code, so we cannot detect them automatically. Therefore we recreate them.
        /// </summary>
        private static void AddInternals()
        {
            entries.Add(new Entry("Folder", ProjectWindowUtil.CreateFolder, () => true, null, 20));
           
            AddScriptTemplate("C# Script", 81, "New Script.cs", UnityScriptTemplates.NewBehaviourScriptPath);
            
            var shaderFolder = AddFolder("Shader", 83);
            AddScriptTemplate("Standard Surface Shader", 83, "NewSurfaceShader.shader", UnityScriptTemplates.StandardSurfaceShader, shaderFolder);
            AddScriptTemplate("Unlit Shader", 83, "NewUnlitShader.shader", UnityScriptTemplates.UnlitShader, shaderFolder);
            AddScriptTemplate("Image Effect Shader", 83, "NewImageEffectShader.shader", UnityScriptTemplates.ImageEffectShader, shaderFolder);
            AddScriptTemplate("Compute Shader", 83, "NewComputeShader.compute", UnityScriptTemplates.ComputeShader, shaderFolder);
            AddInternal(typeof(ShaderVariantCollection), "Shader Variant Collection", 84, "New Shader Variant Collection.shadervariants", () => new ShaderVariantCollection(), shaderFolder);

            var playablesFolder = AddFolder("Playables", 87);
            AddScriptTemplate("Playable Behaviour C# Script", 87, "NewPlayableBehaviour.cs", UnityScriptTemplates.PlayableBehaviour, playablesFolder);
            AddScriptTemplate("Playable Asset C# Script", 88, "NewPlayableAsset.cs", UnityScriptTemplates.PlayableAsset, playablesFolder);
          
            AddScriptTemplate("Assembly Definition", 91, "NewAssembly.asmdef", UnityScriptTemplates.AssemblyDefinition, null, typeof(AssemblyDefinitionAsset));
            #if UNITY_2018_3_OR_NEVER
            AddScriptTemplate("Assembly Definition Reference", 92, "NewAssemblyReference.asmref", UnityScriptTemplates.AssemblyDefinitionReference, null, typeof(AssemblyDefinitionReferenceAsset));
            #endif
            
            entries.Add(new Entry("Scene", ProjectWindowUtil.CreateScene, () => true, null, 201, typeof(Scene)));

            var createAudioMixer =
                typeof(ProjectWindowUtil).GetMethod("CreateAudioMixer", BindingFlags.NonPublic | BindingFlags.Static);
            
            entries.Add(new Entry("Audio Mixer", () => { createAudioMixer.Invoke(null, null); }, () => true, null, 215, typeof(AudioMixer)));
            
            AddInternal(typeof(Material), "Material", 301, "New Material.mat", () => new Material(Shader.Find("Standard")));
            AddInternal(typeof(Flare),"Lens Flare", 303, "New Lens Flare.flare", () => new Flare());
            AddInternal(typeof(RenderTexture),"Render Texture", 304, "New Render Texture.renderTexture", () => new RenderTexture(256, 256, 24));
            AddInternal(typeof(LightmapParameters),"Lightmap Parameters", 305, "New Lightmap Parameters.giparams", () => new LightmapParameters());
            AddInternal(typeof(CustomRenderTexture),"Custom Render Texture", 306, "New Custom Render Texture.asset", () => new CustomRenderTexture(256, 256));
            
            AddInternal(typeof(SpriteAtlas),"Sprite Atlas", 351, "New Sprite Atlas.spriteatlas", () => new SpriteAtlas());
            var sprites = AddFolder("Sprites", 352);
            
            CreateSpritePolygonType createSprite =
                new InternalGetter<CreateSpritePolygonType>(typeof(ProjectWindowUtil), "CreateSpritePolygon").Func;
            
            sprites.Children.Add(new Entry("Square", () => createSprite(0), () => true, sprites, 352, typeof(Sprite)));
            sprites.Children.Add(new Entry("Triangle", () => createSprite(3), () => true, sprites, 352, typeof(Sprite)));
            sprites.Children.Add(new Entry("Diamond", () => createSprite(4), () => true, sprites, 352, typeof(Sprite)));
            sprites.Children.Add(new Entry("Hexagon", () => createSprite(6), () => true, sprites, 352, typeof(Sprite)));
            sprites.Children.Add(new Entry("Circle", () => createSprite(128), () => true, sprites, 352, typeof(Sprite)));
            sprites.Children.Add(new Entry("Polygon", () => createSprite(7), () => true, sprites, 352, typeof(Sprite)));
            
            AddInternal(typeof(AnimatorController),"Animator Controller", 401, "New Animator Controller.controller", () => new AnimatorController());
            AddInternal(typeof(AnimationClip),"Animation", 402, "New Animation.anim", () => new AnimationClip());
            AddInternal(typeof(AnimatorOverrideController),"Animator Override Controller", 403, "New Animator Override Controller.overrideController", () => new AnimatorOverrideController());
            AddInternal(typeof(AvatarMask),"Avatar Mask", 404, "New Avatar Mask.mask", () => new AvatarMask());
            
            AddInternal(typeof(PhysicMaterial),"Physics Material", 501, "New Physic Material.asset", () => new PhysicMaterial());
            AddInternal(typeof(PhysicsMaterial2D),"Physics Material 2D", 502, "New Physic Material 2D.asset", () => new PhysicsMaterial2D());
            
            AddInternal(typeof(Font),"Custom Font", 602, "New Font.fontsettings", () => new Font());
            
            var legacy = AddFolder("Legacy", 850);
            AddInternal(typeof(Cubemap), "Cubemap", 850, "New Cubemap.cubemap", () => new Cubemap(64, TextureFormat.RGBAFloat, true), legacy);
        }

        private static Entry AddFolder(string name, int priority, Entry parent = null)
        {
            var entry = new Entry(name, parent, priority);
            
            if (parent == null)
                entries.Add(entry);
            else
                parent.Children.Add(entry);
            
            return entry;
        }

        private static void AddInternal(Type objectType, string name, int priority, string file, Func<Object> creator, Entry parent = null)
        {
            var entry = new Entry(name, () =>
            {
                ProjectWindowUtil.CreateAsset(creator(), file);
            }, () => true, parent, priority, objectType);
            
            if (parent == null)
                entries.Add(entry);
            else
                parent.Children.Add(entry);
        }

        private delegate void CreateScriptAsset(string templatePath, string destName);
        private static void AddScriptTemplate(string name, int priority, string file, string template, Entry parent = null, Type createdType = null)
        {
#if !UNITY_2019_2_OR_NEVER
            CreateScriptAsset createScriptAsset =
                new InternalGetter<CreateScriptAsset>(typeof(ProjectWindowUtil), "CreateScriptAsset").Func;
#endif
            var entry = new Entry(name, () =>
            {
#if UNITY_2019_2_OR_NEVER
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(template, file);
#else
                createScriptAsset(template, file);
#endif
            }, () => true, parent, priority, createdType);
            
            if (parent == null)
                entries.Add(entry);
            else
                parent.Children.Add(entry);
        }

        private class EntryComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }

        private static Entry EnsureByPath(string path, int defaultPriority, out string itemName)
        {
            var split = path.Split('/');

            if (split.Length == 1)
            {
                itemName = path;
                return null;
            }

            var partial = "";

            Entry lastFolder = null;
            
            for (int i = 0; i < split.Length - 1; ++i)
            {
                partial += split[i] + "/"; 

                if (!byPath.ContainsKey(partial))
                {
                    var folder = new Entry(split[i], lastFolder, defaultPriority);
                    byPath[partial] = folder; 

                    if (lastFolder != null)
                        lastFolder.Children.Add(folder);
                    else
                        entries.Add(folder);
                    
                    lastFolder = folder;
                }
                else
                {
                    lastFolder = byPath[partial];
                }
            }

            itemName = split[split.Length - 1];
            return lastFolder;
        }
        
        private static Dictionary<string, Entry> byPath = new Dictionary<string, Entry>();
        private static List<Entry> entries;

        public static List<Entry> GetEntries() => entries;
    }

    internal class Entry
    {
        public delegate void MenuItemDelegate();
        public delegate bool ValidatorDelegate();
        
        public string Text { get; }
        public bool IsFolder { get; }
        public List<Entry> Children { get; }
        public MenuItemDelegate Create { get; }
        public ValidatorDelegate IsActive { get; }
        public bool IsExpanded { get; set; }
        public bool IsShown { get; set; }
        public int IndentLevel { get; }
        public int Priority { get; }
        public Type CreatedType { get; }
        public GUIContent CachedContent { get; set; }

        public Entry(string name, MenuItemDelegate onCreate, ValidatorDelegate isActive, Entry parent, int priority, Type createdType = null)
        {
            Text = name;
            Create = onCreate;
            IsActive = isActive;
            IsExpanded = true;
            IsFolder = false;
            Children = null;
            IsShown = true;
            IndentLevel = parent?.IndentLevel+1 ?? 0;
            Priority = priority;
            CreatedType = createdType;
        }

        public Entry(string name, Entry parent, int priority)
        {
            Text = name;
            IsExpanded = false;
            IsFolder = true;
            IsActive = () => true;
            Children = new List<Entry>();
            IsShown = true;
            IndentLevel = parent?.IndentLevel+1 ?? 0;
            Priority = priority;
        }
    }
}