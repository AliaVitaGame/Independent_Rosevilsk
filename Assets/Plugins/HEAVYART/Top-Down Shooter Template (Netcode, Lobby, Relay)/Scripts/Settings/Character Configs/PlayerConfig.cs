using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class PlayerConfig
    {
        public string label;

        public GameObject playerPrefab;
        public float movementSpeed = 5;
        public float sprintSpeedMultiplier = 1.6f;
        public float jumpVelocity = 7.5f;
        public float health;

        [Space()]
        public float dropChance;
        public List<PickUpItemController> dropElements;
    }
}

