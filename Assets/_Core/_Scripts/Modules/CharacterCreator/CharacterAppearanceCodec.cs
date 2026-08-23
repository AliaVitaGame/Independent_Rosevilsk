using CC;
using Modules.SaveSystem.Data;
using UnityEngine;

namespace Modules.CharacterCreator
{
    public static class CharacterAppearanceCodec
    {
        public static CharacterAppearanceSaveData ToSaveData(CC_CharacterData source)
        {
            if (source == null)
                return new CharacterAppearanceSaveData();

            var prefab = string.IsNullOrEmpty(source.CharacterPrefab)
                ? source.CharacterName
                : source.CharacterPrefab;

            return new CharacterAppearanceSaveData
            {
                hasValue = true,
                characterName = source.CharacterName ?? string.Empty,
                characterPrefab = prefab ?? string.Empty,
                appearanceJson = JsonUtility.ToJson(source)
            };
        }

        public static CC_CharacterData FromSaveData(CharacterAppearanceSaveData source)
        {
            if (source == null || !source.hasValue)
                return null;

            CC_CharacterData data = null;
            if (!string.IsNullOrEmpty(source.appearanceJson))
                data = JsonUtility.FromJson<CC_CharacterData>(source.appearanceJson);

            data ??= new CC_CharacterData();

            if (string.IsNullOrEmpty(data.CharacterName))
                data.CharacterName = source.characterName;
            if (string.IsNullOrEmpty(data.CharacterPrefab))
                data.CharacterPrefab = source.characterPrefab;

            // Still usable if only prefab/name survived.
            if (string.IsNullOrEmpty(data.CharacterPrefab) && string.IsNullOrEmpty(data.CharacterName))
                return null;

            return data;
        }
    }
}
