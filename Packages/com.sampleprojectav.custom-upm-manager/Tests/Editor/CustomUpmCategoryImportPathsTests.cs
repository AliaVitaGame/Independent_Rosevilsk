using System.Linq;
using NUnit.Framework;

namespace SampleProjectAV.CustomUpmManager.Editor.Tests
{
    public sealed class CustomUpmCategoryImportPathsTests
    {
        [Test]
        public void MapsContentCategoriesToProjectFolders()
        {
            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder("VFX", out var vfx));
            Assert.AreEqual("Assets/_Core/_Content/VFX", vfx);

            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder("models", out var models));
            Assert.AreEqual("Assets/_Core/_Content/Models", models);

            Assert.IsTrue(CustomUpmCategoryImportPaths.TryGetImportFolder("UI", out var ui));
            Assert.AreEqual("Assets/_Core/_Content/Arts/UI", ui);
        }

        [Test]
        public void KeepsCodeAndTemplatesInDefaultAssetsFolder()
        {
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder("Code", out _));
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder("Templates", out _));
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder("Uncategorized", out _));
            Assert.IsFalse(CustomUpmCategoryImportPaths.TryGetImportFolder("Editor Tools", out _));
        }

        [Test]
        public void RemapsImportedAssetsUnderCategoryFolder()
        {
            var remapped = CustomUpmCategoryImportPaths.RemapAssetPath(
                "Assets/Feel/MMFeedbacks.cs",
                CustomUpmCategoryImportPaths.VfxFolder);

            Assert.AreEqual("Assets/_Core/_Content/VFX/Feel/MMFeedbacks.cs", remapped);
        }

        [Test]
        public void DoesNotDoubleNestAssetsAlreadyInDestination()
        {
            var remapped = CustomUpmCategoryImportPaths.RemapAssetPath(
                "Assets/_Core/_Content/VFX/Feel/Effect.prefab",
                CustomUpmCategoryImportPaths.VfxFolder);

            Assert.AreEqual("Assets/_Core/_Content/VFX/Feel/Effect.prefab", remapped);
        }

        [Test]
        public void MovesOnlyNewRootsWhenParentFolderAlreadyExisted()
        {
            var imported = new[]
            {
                "Assets/Plugins/Feel",
                "Assets/Plugins/Feel/Editor",
                "Assets/Plugins/Feel/Editor/FeelEditor.cs",
                "Assets/Feel",
                "Assets/Feel/Feel.cs"
            };

            var moves = CustomUpmCategoryImportPaths.BuildMoves(imported, CustomUpmCategoryImportPaths.VfxFolder);

            Assert.AreEqual(2, moves.Count);
            Assert.AreEqual("Assets/Feel", moves[0].From);
            Assert.AreEqual("Assets/_Core/_Content/VFX/Feel", moves[0].To);
            Assert.AreEqual("Assets/Plugins/Feel", moves[1].From);
            Assert.AreEqual("Assets/_Core/_Content/VFX/Plugins/Feel", moves[1].To);
        }

        [Test]
        public void RemapsAllImportedPathsAfterMove()
        {
            var remapped = CustomUpmCategoryImportPaths.RemapImportedPaths(
                new[]
                {
                    "Assets/TrueShadow/TrueShadow.cs",
                    "Assets/TrueShadow"
                },
                CustomUpmCategoryImportPaths.UiFolder);

            CollectionAssert.AreEqual(
                new[]
                {
                    "Assets/_Core/_Content/Arts/UI/TrueShadow",
                    "Assets/_Core/_Content/Arts/UI/TrueShadow/TrueShadow.cs"
                },
                remapped.ToArray());
        }
    }
}
