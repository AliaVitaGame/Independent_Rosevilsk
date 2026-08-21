using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Analytics.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Playmode.Workflow.Editor
{
    class WindowLayoutPopoutWindow : PopupWindowContent
    {
        private const string k_FrameAfterTopViewSwitch = "frameAfterTopViewSwitch";
        private readonly LayoutCheckboxListView m_layoutCheckboxList;

        public WindowLayoutPopoutWindow()
        {
            m_layoutCheckboxList = new LayoutCheckboxListView();
            m_layoutCheckboxList.OnLayoutFlagsButtonApplied += OnApplyLayoutCheckboxListFlags;
        }

        public override void OnOpen()
        {
            // Always display our toggles reflecting current View Mode State
            var currLayoutFlags = ModeSwitcher.GetLayoutFlagsForMode(EditorApplication.isPlaying);
            m_layoutCheckboxList.RefreshLayout(currLayoutFlags);
            editorWindow.rootVisualElement.Add(m_layoutCheckboxList);
        }

        public override void OnClose()
        {
            editorWindow.rootVisualElement.Clear();
        }

        public override Vector2 GetWindowSize()
        {
            // There doesn't seem to be a way to grab the height of elements dynamically because of how early this
            // is called (basically all the elements are 0 since they are calculating their sizes as well)
            // Use the known size and just dynamically create the final sizes based off of the mode and number of elements.
            // Use +1 for apply button
            return m_layoutCheckboxList.GetLayoutSize();
        }

        internal void OnUpdate()
        {
            if (SessionState.GetBool(k_FrameAfterTopViewSwitch, false))
            {
                SessionState.SetBool(k_FrameAfterTopViewSwitch, false);

                // Switch modes and update our views.
                var isPlayMode = EditorApplication.isPlaying;
                ModeSwitcher.SwitchLayoutToMode(isPlayMode);
                var nextFlags = ModeSwitcher.GetLayoutFlagsForMode(isPlayMode);
                m_layoutCheckboxList.RefreshLayout(nextFlags);
            }
        }

        internal void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var currLayoutFlags = ModeSwitcher.GetLayoutFlagsForMode(EditorApplication.isPlaying);
            m_layoutCheckboxList.RefreshLayout(currLayoutFlags);
        }

        private void OnApplyLayoutCheckboxListFlags(LayoutFlags nextFlags)
        {
#if UNITY_2023_2_OR_NEWER
            // If applying new flags, schedule the next layout flags to update Mode Switcher.
            // Close the Flags window whenever layout flags are applied
            if (ModeSwitcher.SetLayoutFlagsForMode(EditorApplication.isPlaying, nextFlags))
            {
                var layoutWindows = UpdateLayoutWindows(nextFlags);

                AnalyticsLayoutChangedEvent.Send(new LayoutChangedData()
                {
                    LayoutWindows = layoutWindows.ToArray(),
                    IsPlayMode = EditorApplication.isPlaying
                });
                SessionState.SetBool(k_FrameAfterTopViewSwitch, true);
                EditorWindow.focusedWindow?.Close();
            }
#endif
        }

        private static List<LayoutWindowData> UpdateLayoutWindows(LayoutFlags nextFlags)
        {
            var layoutWindows = new List<LayoutWindowData>();
            foreach (LayoutFlags flag in Enum.GetValues(typeof(LayoutFlags)))
            {
                if (flag != LayoutFlags.None)
                {
                    bool isChecked = (nextFlags & flag) == flag;
                    layoutWindows.Add(new LayoutWindowData
                    {
                        Name = Enum.GetName(typeof(LayoutFlags), flag),
                        Active = isChecked
                    });
                }
            }

            return layoutWindows;
        }
    }
}
