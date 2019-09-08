using UnityEditor;
using UnityEngine;

namespace CreateWindow
{
    internal static class Styles
    {
        public static readonly int HEADER_HEIGHT = 30;
        public static readonly int ITEM_HEIGHT = 28;
        public static readonly int INDENT = 20;
        
        public static GUIStyle itemStyle;
        public static GUIStyle itemStyleInactive;
        
        public static GUIStyle foldIconClosed;
        public static GUIStyle foldIconOpen;
        
        public static GUIStyle toolbarSearch;
        public static GUIStyle toolbarCancel;
        public static GUIStyle grayBorder;
        public static GUIStyle toolbar;
        
        static Styles()
        {
            toolbarSearch = "toolbarSeachTextField";
            toolbarCancel = "toolbarSeachCancelButton";
            toolbar = new GUIStyle("toolbar");
            toolbar.fixedHeight = 0;
            grayBorder = "grey_border";
            
            itemStyle = new GUIStyle();
            itemStyle.normal.background =  GenerateTexture(new Color(1, 1, 1,0));
            itemStyle.hover.background = GenerateTexture(new Color(17 / 255.0f, 88/ 255.0f, 158/ 255.0f));
            itemStyle.hover.textColor = Color.white;

            itemStyle.alignment = TextAnchor.MiddleLeft;
            itemStyle.padding.left = 10;
            itemStyle.padding.top = 4;
            itemStyle.padding.bottom = 4;
            
            itemStyleInactive = new GUIStyle();
            itemStyleInactive.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
            itemStyleInactive.alignment = TextAnchor.MiddleLeft;
            itemStyleInactive.padding.top = 4;
            itemStyleInactive.padding.bottom = 4;

            foldIconClosed = new GUIStyle();
            foldIconClosed.normal.background = Resources.Load<Texture2D>("FoldClosed");
            foldIconClosed.hover.background = Resources.Load<Texture2D>("FoldClosed Hover");
            foldIconClosed.fixedWidth = 10;
            foldIconClosed.fixedHeight = 28;
            
            foldIconOpen = new GUIStyle();
            foldIconOpen.normal.background = Resources.Load<Texture2D>("FoldOpen");
            foldIconOpen.hover.background = Resources.Load<Texture2D>("FoldOpen Hover");
            foldIconOpen.fixedWidth = 10;
            foldIconOpen.fixedHeight = 28;
        }

        private static Texture2D GenerateTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
        
    }
}