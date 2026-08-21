using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    internal static class UIUtils
    {
        internal static void ApplyStyleSheet(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"Style asset '{path}' does not exist.");

            var loadAssetAtPath = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            visualElement.styleSheets.Add(loadAssetAtPath);
        }

        internal static async Task ApplyStyleSheetAsync(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"Style asset '{path}' does not exist.");

            var loadAssetAtPath = await AssetDatabaseHelper.LoadAssetAtPathAsync<StyleSheet>(path);
            visualElement.styleSheets.Add(loadAssetAtPath);
        }

        internal static async Task LoadUxmlAsync(string relativePath, VisualElement visualElement)
        {
            var path = $"{Constants.k_PackageRoot}{relativePath}";
            Assert.IsTrue(AssetDatabase.AssetPathExists(path), $"UXML asset '{path}' does not exist.");


            var loadAssetAtPath = await AssetDatabaseHelper.LoadAssetAtPathAsync<VisualTreeAsset>(path);
            loadAssetAtPath.CloneTree(visualElement);
        }

        internal static void Spin(VisualElement element)
        {
            const int k_UpdateIntervalMS = 32;
            const float k_RotationSpeedDegSec = 360f;

            var startTime = EditorApplication.timeSinceStartup;

            element.schedule.Execute(_ =>
            {
                var elapsedTime = (float)(EditorApplication.timeSinceStartup - startTime);
                element.style.rotate = new StyleRotate(new Rotate(new Angle(k_RotationSpeedDegSec * elapsedTime, AngleUnit.Degree)));
            }).Every(k_UpdateIntervalMS);
        }
    }
}
