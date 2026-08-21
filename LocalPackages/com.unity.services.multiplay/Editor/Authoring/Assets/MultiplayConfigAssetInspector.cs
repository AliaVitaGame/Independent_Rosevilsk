using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    [CustomEditor(typeof(MultiplayConfigAsset))]
    [CanEditMultipleObjects]
    class MultiplayConfigAssetInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            DisableReadonlyFlags();
            return new VisualElement();
        }

        void DisableReadonlyFlags()
        {
            serializedObject.targetObject.hideFlags = HideFlags.None;
        }
    }
}
