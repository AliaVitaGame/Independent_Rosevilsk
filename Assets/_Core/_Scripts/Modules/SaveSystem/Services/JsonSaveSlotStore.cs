using System;
using System.Collections.Generic;
using System.IO;
using Modules.SaveSystem.Data;
using UnityEngine;

namespace Modules.SaveSystem.Services
{
    public sealed class JsonSaveSlotStore
    {
        public const int SlotCount = 3;
        private const string LastSlotKey = "GameSave.LastPlayedSlot";

        private readonly string _rootPath;

        public JsonSaveSlotStore()
        {
            _rootPath = Path.Combine(Application.persistentDataPath, "SaveSlots");
            Directory.CreateDirectory(_rootPath);
        }

        public int LastPlayedSlotIndex
        {
            get => PlayerPrefs.GetInt(LastSlotKey, -1);
            set
            {
                PlayerPrefs.SetInt(LastSlotKey, value);
                PlayerPrefs.Save();
            }
        }

        public bool Exists(int slotIndex) => File.Exists(GetPath(slotIndex));

        public GameSaveData Load(int slotIndex)
        {
            var path = GetPath(slotIndex);
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.savedAtUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(GetPath(data.slotIndex), JsonUtility.ToJson(data, true));
            LastPlayedSlotIndex = data.slotIndex;
        }

        public IReadOnlyList<SaveSlotInfo> GetSlots()
        {
            var list = new List<SaveSlotInfo>(SlotCount);
            for (var i = 0; i < SlotCount; i++)
                list.Add(ToInfo(i, Load(i)));
            return list;
        }

        public SaveSlotInfo GetSlot(int slotIndex) => ToInfo(slotIndex, Load(slotIndex));

        private string GetPath(int slotIndex) =>
            Path.Combine(_rootPath, $"slot_{slotIndex}.json");

        private static SaveSlotInfo ToInfo(int slotIndex, GameSaveData data)
        {
            if (data == null)
            {
                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    isEmpty = true
                };
            }

            return new SaveSlotInfo
            {
                slotIndex = slotIndex,
                isEmpty = false,
                savedAtUtc = data.savedAtUtc,
                playtimeSeconds = data.playtimeSeconds
            };
        }
    }
}
