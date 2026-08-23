using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Passengers;
using Game.Core.Vehicles;
using HEAVYART.TopDownShooter.Netcode;
using Modules.SaveSystem.Data;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;

namespace Modules.SaveSystem.Services
{
    public sealed class GameSaveService : IGameSaveService
    {
        private readonly JsonSaveSlotStore _store = new();
        private readonly ICurrencyService _currencyService;

        private int _activeSlotIndex = -1;
        private GameSaveData _activeData;
        private GameSaveData _pendingLoad;
        private float _playtimeAnchorRealtime;

        public GameSaveService(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public int SlotCount => JsonSaveSlotStore.SlotCount;
        public int ActiveSlotIndex => _activeSlotIndex;
        public int LastPlayedSlotIndex => _store.LastPlayedSlotIndex;

        public bool HasAnySave()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (_store.Exists(i))
                    return true;
            }

            return false;
        }

        public IReadOnlyList<SaveSlotInfo> GetSlots() => _store.GetSlots();
        public SaveSlotInfo GetSlot(int slotIndex) => _store.GetSlot(slotIndex);

        public GameSaveData BeginNewGame(int slotIndex, CharacterAppearanceSaveData appearance = null)
        {
            ValidateSlot(slotIndex);

            _activeData = new GameSaveData
            {
                slotIndex = slotIndex,
                playtimeSeconds = 0f,
                sceneName = "Game",
                appearance = CloneAppearance(appearance)
            };

            WriteDefaultCurrencies(_activeData);
            _store.Save(_activeData);

            _activeSlotIndex = slotIndex;
            _store.LastPlayedSlotIndex = slotIndex;
            // New games still need world defaults applied once the player spawns.
            _pendingLoad = Clone(_activeData);
            _playtimeAnchorRealtime = Time.realtimeSinceStartup;
            return _activeData;
        }

        public CharacterAppearanceSaveData GetActiveAppearance()
        {
            var source = _pendingLoad?.appearance ?? _activeData?.appearance;
            if (source != null && source.hasValue)
                return CloneAppearance(source);

            // Fallback: disk may still have appearance after a respawn / domain reload.
            if (_activeSlotIndex >= 0)
            {
                var disk = _store.Load(_activeSlotIndex);
                if (disk?.appearance != null && disk.appearance.hasValue)
                {
                    if (_activeData != null)
                        _activeData.appearance = CloneAppearance(disk.appearance);
                    return CloneAppearance(disk.appearance);
                }
            }

            return new CharacterAppearanceSaveData();
        }

        public GameSaveData BeginLoadGame(int slotIndex)
        {
            ValidateSlot(slotIndex);
            var data = _store.Load(slotIndex)
                        ?? throw new InvalidOperationException($"Save slot {slotIndex} is empty.");

            _activeSlotIndex = slotIndex;
            _activeData = data;
            _pendingLoad = Clone(data);
            _playtimeAnchorRealtime = Time.realtimeSinceStartup;
            _store.LastPlayedSlotIndex = slotIndex;
            return data;
        }

        public bool TryBeginContinue(out GameSaveData data)
        {
            data = null;
            var last = _store.LastPlayedSlotIndex;
            if (last < 0 || last >= SlotCount || !_store.Exists(last))
                last = FindNewestSlotIndex();

            if (last < 0)
                return false;

            data = BeginLoadGame(last);
            return true;
        }

        public void CaptureAndSaveActiveSlot()
        {
            if (_activeSlotIndex < 0)
                return;

            _activeData ??= new GameSaveData { slotIndex = _activeSlotIndex };
            _activeData.slotIndex = _activeSlotIndex;
            AccumulatePlaytime();
            CaptureWorld(_activeData);
            _store.Save(_activeData);
            Debug.Log($"[GameSave] Saved slot {_activeSlotIndex}.");
        }

        public bool TryConsumePendingLoad(out GameSaveData data)
        {
            data = _pendingLoad;
            if (data == null)
                return false;

            _pendingLoad = null;
            return true;
        }

        public void ApplyPendingLoadToWorld()
        {
            var data = _pendingLoad ?? _activeData;
            _pendingLoad = null;
            if (data == null)
                return;

            ApplyWorld(data);
            _activeData = data;
            _activeSlotIndex = data.slotIndex;
            _playtimeAnchorRealtime = Time.realtimeSinceStartup;
        }

        private void CaptureWorld(GameSaveData data)
        {
            CaptureCurrencies(data);
            CapturePlayer(data);
            CaptureVehicle(data);
            CapturePassengers(data);
        }

        private void ApplyWorld(GameSaveData data)
        {
            ApplyCurrencies(data);
            ApplyPlayer(data);
            ApplyAppearance(data);
            ApplyVehicle(data);
            ApplyPassengers(data);
        }

        private static void ApplyAppearance(GameSaveData data)
        {
            if (data?.appearance == null || !data.appearance.hasValue)
                return;

            var appearance = Modules.CharacterCreator.CharacterAppearanceCodec.FromSaveData(data.appearance);
            if (appearance == null)
                return;

            Modules.CharacterCreator.PlayerAppearanceApplier.ApplyToLocalPlayer(appearance);
        }

        private void CaptureCurrencies(GameSaveData data)
        {
            data.currencies ??= new List<CurrencySaveEntry>();
            data.currencies.Clear();
            if (_currencyService == null)
                return;

            foreach (var pair in _currencyService.GetAllCurrencies())
            {
                if (pair.Value == null)
                    continue;

                data.currencies.Add(new CurrencySaveEntry
                {
                    type = pair.Key.ToString(),
                    amount = pair.Value.Value
                });
            }
        }

        private void ApplyCurrencies(GameSaveData data)
        {
            if (_currencyService == null || data.currencies == null)
                return;

            foreach (var entry in data.currencies)
            {
                if (!Enum.TryParse(entry.type, true, out CurrencyType type) || type == CurrencyType.None)
                    continue;

                _currencyService.SetCurrency(type, entry.amount, entry.max);
            }
        }

        private static void CapturePlayer(GameSaveData data)
        {
            data.player ??= new PlayerSaveData();
            data.appearance ??= new CharacterAppearanceSaveData();
            var player = FindLocalPlayerTransform();
            if (player == null)
            {
                data.player.hasValue = false;
                return;
            }

            data.player.hasValue = true;
            data.player.position = player.position;
            data.player.rotationEuler = player.rotation.eulerAngles;
        }

        private static void ApplyPlayer(GameSaveData data)
        {
            if (data.player == null || !data.player.hasValue)
                return;

            var player = FindLocalPlayerTransform();
            if (player == null)
                return;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = data.player.position;
                rb.rotation = Quaternion.Euler(data.player.rotationEuler);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                player.SetPositionAndRotation(
                    data.player.position,
                    Quaternion.Euler(data.player.rotationEuler));
            }
        }

        private static void CaptureVehicle(GameSaveData data)
        {
            data.vehicle ??= new VehicleSaveData();
            var vehicle = UnityEngine.Object.FindFirstObjectByType<DriveableVehicleInteraction>();
            if (vehicle == null)
            {
                data.vehicle.hasValue = false;
                return;
            }

            data.vehicle.hasValue = true;
            data.vehicle.position = vehicle.transform.position;
            data.vehicle.rotationEuler = vehicle.transform.rotation.eulerAngles;
            data.vehicle.isDriving = vehicle.IsDriving;
        }

        private static void ApplyVehicle(GameSaveData data)
        {
            if (data.vehicle == null || !data.vehicle.hasValue)
                return;

            var vehicle = UnityEngine.Object.FindFirstObjectByType<DriveableVehicleInteraction>();
            if (vehicle == null)
                return;

            var rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = data.vehicle.position;
                rb.rotation = Quaternion.Euler(data.vehicle.rotationEuler);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                vehicle.transform.SetPositionAndRotation(
                    data.vehicle.position,
                    Quaternion.Euler(data.vehicle.rotationEuler));
            }
        }

        private static void CapturePassengers(GameSaveData data)
        {
            data.passengers ??= new PassengerSaveData();
            var system = UnityEngine.Object.FindFirstObjectByType<PassengerPickupSystem>();
            data.passengers.boardedCount = system != null ? system.BoardedCount : 0;
        }

        private static void ApplyPassengers(GameSaveData data)
        {
            if (data.passengers == null)
                return;

            var system = UnityEngine.Object.FindFirstObjectByType<PassengerPickupSystem>();
            system?.RestoreBoardedCount(data.passengers.boardedCount);
        }

        private static Transform FindLocalPlayerTransform()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerBehaviour>(FindObjectsInactive.Exclude);
            foreach (var player in players)
            {
                if (player != null && player.IsOwner)
                    return player.transform;
            }

            var controllers = UnityEngine.Object.FindObjectsByType<RigidbodyCharacterController>(FindObjectsInactive.Exclude);
            foreach (var controller in controllers)
            {
                if (controller != null && controller.IsOwner)
                    return controller.transform;
            }

            return null;
        }

        private void WriteDefaultCurrencies(GameSaveData data)
        {
            data.currencies = new List<CurrencySaveEntry>
            {
                new() { type = nameof(CurrencyType.Cash), amount = 0f },
                new() { type = nameof(CurrencyType.Diamond), amount = 0f },
                new() { type = nameof(CurrencyType.Energy), amount = 0f }
            };

            if (_currencyService == null)
                return;

            _currencyService.SetCurrency(CurrencyType.Cash, 0f);
            _currencyService.SetCurrency(CurrencyType.Diamond, 0f);
            _currencyService.SetCurrency(CurrencyType.Energy, 0f);
        }

        private void AccumulatePlaytime()
        {
            if (_activeData == null)
                return;

            var now = Time.realtimeSinceStartup;
            _activeData.playtimeSeconds += Mathf.Max(0f, now - _playtimeAnchorRealtime);
            _playtimeAnchorRealtime = now;
        }

        private int FindNewestSlotIndex()
        {
            var bestIndex = -1;
            var bestTime = DateTime.MinValue;

            for (var i = 0; i < SlotCount; i++)
            {
                var data = _store.Load(i);
                if (data == null || string.IsNullOrEmpty(data.savedAtUtc))
                    continue;

                if (!DateTime.TryParse(data.savedAtUtc, null, DateTimeStyles.RoundtripKind, out var time))
                    continue;

                if (time <= bestTime)
                    continue;

                bestTime = time;
                bestIndex = i;
            }

            return bestIndex;
        }

        private static void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= JsonSaveSlotStore.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        private static GameSaveData Clone(GameSaveData source) =>
            JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));

        private static CharacterAppearanceSaveData CloneAppearance(CharacterAppearanceSaveData source)
        {
            if (source == null || !source.hasValue)
                return new CharacterAppearanceSaveData();

            return JsonUtility.FromJson<CharacterAppearanceSaveData>(JsonUtility.ToJson(source));
        }
    }
}
