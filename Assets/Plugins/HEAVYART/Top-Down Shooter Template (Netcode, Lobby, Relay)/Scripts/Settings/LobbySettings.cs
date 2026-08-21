using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class LobbySettings : MonoBehaviour
    {
        public int defaultPlayerCount = 2;

        public int minPlayers = 2;
        public int maxPlayers = 4;

        [Header("Timeouts (ms)")]
        public int waitForPlayersToInitializeDelay = 2000;
        public int waitForPlayersReadyResponseDelay = 2000;
        public int waitForPlayersToRemoveDelay = 2000;

        [Space()]
        public int lobbyHeartbeatRate = 20000;
        public int autoRefreshRate = 10000;

        [Space()]
        public int regionsUpdateRateHours = 24;
    }
}
