using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class BoneAngleAdjuster : MonoBehaviour
    {
        public Vector3 offset;

        // Update is called once per frame
        void LateUpdate()
        {
            if (offset != Vector3.zero)
                transform.localRotation *= Quaternion.Euler(offset);
        }
    }
}
