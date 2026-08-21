using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CameraSettings : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 8, -6);
        public float angle = 55;
        public float dampTime = 0.05f;
    }
}
