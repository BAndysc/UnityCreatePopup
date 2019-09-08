using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// In MenuItem method I cannot open popupwindow as I don't know mouse pointer location (as Event.current is null)
/// so I open menu in the next event, however how to detect next event? I could create EditorWindow, but it would require
/// to have that window opened all the time. So here is the workaround: using reflection we replace processEvent delegate in GUIUtility
/// to show the menu when needed
/// </summary>
namespace CreateWindow
{
    internal class CreateNewContextMenu
    {
        private static bool showContextMenuInNextEvent;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            var field = typeof(GUIUtility).GetField("processEvent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            Debug.Assert(field != null, "Cannot find processEvent delegate in GUIUtility. Are you using unsupported version of Unity?");
            
            var oldProcessEvent = (Func<int, IntPtr, bool>) field.GetValue(null);

            Func<int, IntPtr, bool> newProcessEvent = (a, b) =>
            {
                if (showContextMenuInNextEvent)
                {
                    showContextMenuInNextEvent = false;

                    PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0),
                        new CreatePopupWindowContent());
                }

                return oldProcessEvent(a, b);
            };

            field.SetValue(null, newProcessEvent);
        }

        [MenuItem("Assets/Create New", false, -1)]
        private static void DoSomething()
        {
            showContextMenuInNextEvent = true;
        }
    }
}