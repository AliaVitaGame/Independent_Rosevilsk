using System.Collections.Generic;
using Modules.SaveSystem.Data;

namespace Modules.SaveSystem
{
    public interface IGameSaveService
    {
        int SlotCount { get; }
        int ActiveSlotIndex { get; }
        int LastPlayedSlotIndex { get; }

        bool HasAnySave();
        IReadOnlyList<SaveSlotInfo> GetSlots();
        SaveSlotInfo GetSlot(int slotIndex);

        GameSaveData BeginNewGame(int slotIndex);
        GameSaveData BeginLoadGame(int slotIndex);
        bool TryBeginContinue(out GameSaveData data);

        void CaptureAndSaveActiveSlot();
        bool TryConsumePendingLoad(out GameSaveData data);
        void ApplyPendingLoadToWorld();
    }
}
