using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CommonSettings : MonoBehaviour
    {
        public int targetFPS = 60;

        //Used to split players between different versions of product
        public string projectVersion;

        void Start()
        {
            Application.targetFrameRate = targetFPS;
        }
    }
}
