using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.SaveSystem.Data
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public int slotIndex;
        public string savedAtUtc;
        public float playtimeSeconds;
        public string sceneName = "Game";

        public PlayerSaveData player = new();
        public VehicleSaveData vehicle = new();
        public PassengerSaveData passengers = new();
        public List<CurrencySaveEntry> currencies = new();
        public List<StringEntry> extras = new();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public bool hasValue;
        public Vector3 position;
        public Vector3 rotationEuler;
    }

    [Serializable]
    public sealed class VehicleSaveData
    {
        public bool hasValue;
        public Vector3 position;
        public Vector3 rotationEuler;
        public bool isDriving;
    }

    [Serializable]
    public sealed class PassengerSaveData
    {
        public int boardedCount;
    }

    [Serializable]
    public sealed class CurrencySaveEntry
    {
        public string type;
        public float amount;
        public float max;
    }

    [Serializable]
    public sealed class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class SaveSlotInfo
    {
        public int slotIndex;
        public bool isEmpty = true;
        public string savedAtUtc;
        public float playtimeSeconds;

        public string FormatPlaytime()
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(playtimeSeconds));
            var hours = total / 3600;
            var minutes = total % 3600 / 60;
            return hours > 0 ? $"{hours}h {minutes:00}m" : $"{minutes}m";
        }

        public string FormatSavedAtLocal()
        {
            if (string.IsNullOrEmpty(savedAtUtc))
                return string.Empty;

            if (!DateTime.TryParse(savedAtUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var utc))
                return savedAtUtc;

            return utc.ToLocalTime().ToString("g");
        }
    }
}
