using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class AISettings : MonoBehaviour
    {
        public List<AIConfig> configs = new List<AIConfig>();

        public ulong defaultOwnerID => 1000;
    }
}
