using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class AIConfig
    {
        public string label;

        public GameObject botPrefab;
        public float movementSpeed;
        public float health;

        [Space()]
        public float distanceToOpenFire = 9;
        public float targetUpdateRate = 1;
        public float maneuverAngle = 120;
        public float minDistanceOfManeuver = 2;
        public float maxDistanceOfManeuver = 10;
        public float minDistanceToUpdateManeuverPoint = 0.5f;
        public float maneuverExitTime = 3;

        [Space()]
        public float dropChance;
        public List<PickUpItemController> dropElements;
    }
}
