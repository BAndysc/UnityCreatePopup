using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CreateWindow;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace CreateWindow
{
    internal class CreatePopupWindowContent : PopupWindowContent
    {
        private string searchText;
    
        private bool first = true;
        private int y = 0;
        private Rect contentRect;
    
        private Vector2 scroll;
        
        private List<Entry> entries;
    
        public CreatePopupWindowContent()
        {
            entries = CreateItemsProvider.GetEntries();
            CollapseAll(entries, true);
            FillContent(entries);
        }
        
        public override Vector2 GetWindowSize()
        {
            return new Vector2(320, 520);
        }
    
        private void FillContent(IEnumerable<Entry> list)
        {
            foreach (var entry in list)
            {
                if (entry.IsFolder)
                {
                    entry.CachedContent = new GUIContent(entry.Text);
                    FillContent(entry.Children);
                }
                else
                {
                    entry.CachedContent = GetContentForType(entry.Text, entry.CreatedType);
                }
            }
        } 
        
        private void UnfilterAll(IEnumerable<Entry> list, bool isShown)
        {
            foreach (var entry in list)
            {
                entry.IsShown = isShown;
                if (entry.IsFolder)
                    UnfilterAll(entry.Children, isShown);
            }
        } 
    
        private void FilterAll()
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                UnfilterAll(entries, false);
                CalculateIsShown(entries);
    
                foreach (var entry in entries)
                    AnyChildrenIsShown(entry);
    
                var shownCount = entries.Count(entry => entry.IsShown);
                if (shownCount == 1)
                {
                    CollapseAllShown(entries, false);
                }
            }
            else
                UnfilterAll(entries, true);
        }
    
        private bool AnyChildrenIsShown(Entry entry)
        {
            bool any = false;
    
            if (!entry.IsFolder)
                return entry.IsShown;
    
            foreach (var child in entry.Children)
                any |= AnyChildrenIsShown(child);
    
            entry.IsShown |= any;
            
            return any;
        }
    
        private void CalculateIsShown(List<Entry> list)
        {
            foreach (var entry in list)
            {
                entry.IsShown = entry.Text.ToLower().Contains(searchText);
    
                if (entry.IsFolder)
                    CalculateIsShown(entry.Children);
            }
        }
    
        private void CollapseAll(IEnumerable<Entry> list, bool collapse)
        {
            foreach (var entry in list)
            {
                entry.IsExpanded = !collapse;
                if (entry.IsFolder)
                    CollapseAll(entry.Children, collapse);
            }
        }
        
        private void CollapseAllShown(IEnumerable<Entry> list, bool collapse)
        {
            foreach (var entry in list)
            {
                if (!entry.IsShown)
                    continue;
                
                entry.IsExpanded = !collapse;
                if (entry.IsFolder)
                    CollapseAllShown(entry.Children, collapse);
            }
        }
    
        private int CalculateHeight(IEnumerable<Entry> list)
        {
            int height = 0;
            foreach (var entry in list)
            {
                if (!entry.IsShown)
                    continue;
                
                height += Styles.ITEM_HEIGHT;
                if (entry.IsFolder && entry.IsExpanded)
                    height += CalculateHeight(entry.Children);
            }
    
            return height;
        }
    
        public override void OnGUI(Rect rect)
        {
            Rect headerRect;
            rect.SplitVertical(Styles.HEADER_HEIGHT, out headerRect, out contentRect);
    
            DrawHeader(headerRect);
            DrawContent();
    
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
            {
                var firstShown = GetFirstShown(entries);
                firstShown.Create();
            }
            
            // always repaint for responsiveness
            editorWindow.Repaint();
        }
    
        private Entry GetFirstShown(List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                if (!entry.IsShown) 
                    continue;
                
                if (!entry.IsFolder) 
                    return entry;
                
                var found = GetFirstShown(entry.Children);
                
                if (found != null)
                    return found;

                return entry;
            }
    
            return null;
        }
    
        private void DrawHeader(Rect headerRect)
        {
            if (Event.current.type == EventType.Repaint)
                Styles.toolbar.Draw(headerRect, GUIContent.none, false, false, false, false);
            
            headerRect = headerRect.Padding(((int)headerRect.height - (int)Styles.toolbarSearch.fixedHeight) / 2);

            Rect searchRect;
            Rect cancelRect;
            headerRect.SplitHorizontal((int)headerRect.width - (int)Styles.toolbarCancel.fixedWidth, out searchRect, out cancelRect);
    
            GUI.SetNextControlName(nameof(searchText));
            searchText = EditorGUI.TextField(searchRect, searchText, Styles.toolbarSearch);
            GUI.Button(cancelRect, GUIContent.none, Styles.toolbarCancel);

            if (!first) 
                return;
            
            GUI.FocusControl(nameof(searchText));
            first = false;
        }
    
        private void DrawContent()
        {
            FilterAll();
            
            scroll = GUI.BeginScrollView(contentRect, scroll, new Rect(-scroll.x, -scroll.y, contentRect.width - 20, CalculateHeight(entries)));
            
            y = 0;
            prevPriority = -1;
            PaintRecur(entries);
            
            GUI.EndScrollView();
        }
    
        private void PaintRecur(List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                if (!entry.IsShown)
                    continue;
                
                Profiler.BeginSample("Draw single");
                DrawItemLayout(entry);
                Profiler.EndSample();
                if (entry.IsExpanded && entry.IsFolder)
                    PaintRecur(entry.Children);
            }
        }
    
        private int prevPriority = -1;
        private void DrawItemLayout(Entry entry)
        {
            if (prevPriority / 50 != entry.Priority / 50 || prevPriority < 20 && entry.Priority >= 20)
            {
                if (Event.current.type == EventType.Repaint)
                    Styles.grayBorder.Draw(new Rect(0, y -scroll.y, contentRect.width, 1), GUIContent.none, false, false, false, false);
            }
            
            var rect = new Rect(-scroll.x, y - scroll.y, contentRect.width, Styles.ITEM_HEIGHT);
            y += Styles.ITEM_HEIGHT;
            Styles.itemStyle.padding.left = Styles.itemStyleInactive.padding.left = entry.IndentLevel * Styles.INDENT + 10;
    
            var active = entry.IsActive();
    
            if (!active) 
                GUI.Label(rect, entry.CachedContent, Styles.itemStyleInactive);
            else if (entry.IsFolder)
            {
                var content = new GUIContent(entry.Text);
                content.image = entry.IsExpanded
                    ? Styles.foldIconOpen.normal.background
                    : Styles.foldIconClosed.normal.background;
                
                if (GUI.Button(rect, content, Styles.itemStyle))
                    entry.IsExpanded = !entry.IsExpanded;
            }
            else
            {
                if (GUI.Button(rect, entry.CachedContent, Styles.itemStyle))
                {
                    if (entry.IsFolder)
                    {
                        entry.IsExpanded = !entry.IsExpanded;
                    }
                    else
                    {
                        editorWindow.Close();
                        entry.Create();
                    }
                }
            }
    
            prevPriority = entry.Priority;
        }
    
        private Dictionary<string, Texture> icons = new Dictionary<string, Texture>();
        private HashSet<string> important = new HashSet<string>(){"cs Script Icon", "ScriptableObject Icon", "TimelineAsset Icon", "Folder Icon",  "SignalAsset Icon", "SceneAsset Icon", "AudioMixerController Icon", "Prefab Icon", "Shader Icon"};
        private bool init;
    
        private delegate Texture2D LoadIconDelegate(string name);
        
        private Texture GetTextureByName(string name)
        {
            if (!init)
            {
                init = true;
    
                LoadIconDelegate loadIcon = new InternalGetter<LoadIconDelegate>(typeof(EditorGUIUtility), "LoadIcon").Func;
                
                foreach (var icon in important)
                {
                    var t = loadIcon(icon);
                    icons.Add(icon, t);
                }
            }

            Texture tex;
            icons.TryGetValue(name, out tex);
            return tex;
        }
        
        private GUIContent GetContentForType(string text, Type type)
        {
            var content = new GUIContent(ObjectNames.NicifyVariableName(text), GetTextureByName("ScriptableObject Icon"));
            
            if (text == "Timeline")
                content.image = GetTextureByName("TimelineAsset Icon");
            else if (text == "Folder" || 
                     text == "Tests Assembly Folder")
                content.image = GetTextureByName("Folder Icon");
            else if (text == "Signal")
                    content.image = GetTextureByName("SignalAsset Icon");
            else if (text == "Prefab Variant") 
                content.image = GetTextureByName("Prefab Icon");
            else if ((text == "Standard Surface Shader") ||
                     (text == "Unlit Shader") ||
                     (text == "Image Effect Shader") ||
                     (text == "Compute Shader"))
                content.image = GetTextureByName("Shader Icon");
            else if ((text == "C# Script") ||
                     (text == "Playable Behaviour C# Script") ||
                     (text == "Playable Asset C# Script") ||
                     (text == "C# Test Script"))
                content.image = GetTextureByName("cs Script Icon");
            else if (type == typeof(AudioMixer))
            {
                // workaround: ObjectContent(null, typeof(AudioMixer)) returns wrong icon
                content.image = GetTextureByName("AudioMixerController Icon");
                return content;
            }
            else if (type == typeof(Scene))
            {
                content.image = GetTextureByName("SceneAsset Icon");
                return content;
            }
            else if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
            {
                // workaround: Unity doesnt return correct icon in ObjectContent with SO
                var inst = ScriptableObject.CreateInstance(type);
                content.image = AssetPreview.GetMiniThumbnail(inst);
                UnityEngine.Object.DestroyImmediate(inst);
                return content; 
            }
              
            if (type == null)
                return content;
     
            content.image = EditorGUIUtility.ObjectContent(null, type).image;
            
            return content;
        }
    }
}