using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Core.UI
{
    public static class GameplayUiStyle
    {
        public static readonly Color AccentRed = new(0.72f, 0.08f, 0.08f, 1f);
        public static readonly Color TextLight = new(0.86f, 0.86f, 0.84f, 1f);
        public static readonly Color TextDim = new(0.55f, 0.55f, 0.53f, 1f);
        public static readonly Color PanelBg = new(0.03f, 0.03f, 0.035f, 0.86f);

        private const string Bloodlines = "Assets/_Core/_Content/Arts/UI/Bloodlines UI/";

        public static TMP_FontAsset LoadTitleFont()
        {
            return LoadFont(Bloodlines + "Fonts/ManufacturingConsent SDF.asset")
                   ?? LoadFont(Bloodlines + "Fonts/MedievalSharp SDF.asset")
                   ?? LoadFont("Assets/CharacterCustomizer/UI/Fonts/Roboto/Roboto-Bold SDF.asset")
                   ?? TMP_Settings.defaultFontAsset;
        }

        public static TMP_FontAsset LoadBodyFont()
        {
            return LoadFont("Assets/CharacterCustomizer/UI/Fonts/Roboto/Roboto-Bold SDF.asset")
                   ?? LoadFont("Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")
                   ?? LoadTitleFont();
        }

        public static Sprite LoadFrame()
        {
            return LoadSprite(Bloodlines + "Textures/Frame/Frame_outline_red.png")
                   ?? LoadSprite(Bloodlines + "Textures/Frame/Frame_outline.png");
        }

        public static Sprite LoadBarEmpty()
        {
            return LoadSprite(Bloodlines + "Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty_v1.png");
        }

        public static Sprite LoadBarFull()
        {
            return LoadSprite(Bloodlines + "Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v1.png");
        }

        public static AudioClip LoadProjectMusic()
        {
            return LoadClip("Assets/_Core/_Content/Audios/Musics/Music_Rose.mp3", "Music_Rose");
        }

        public static AudioClip LoadClip(string path, string fallbackName = null)
        {
            return LoadAsset<AudioClip>(path, fallbackName);
        }

        private static TMP_FontAsset LoadFont(string path)
        {
            return LoadAsset<TMP_FontAsset>(path, null);
        }

        private static Sprite LoadSprite(string path)
        {
            return LoadAsset<Sprite>(path, null);
        }

        private static T LoadAsset<T>(string path, string fallbackName) where T : Object
        {
#if UNITY_EDITOR
            var fromPath = AssetDatabase.LoadAssetAtPath<T>(path);
            if (fromPath != null)
                return fromPath;
#endif
            if (string.IsNullOrEmpty(fallbackName))
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                fallbackName = fileName;
            }

            var loaded = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].name == fallbackName)
                    return loaded[i];
            }

            return null;
        }
    }
}
