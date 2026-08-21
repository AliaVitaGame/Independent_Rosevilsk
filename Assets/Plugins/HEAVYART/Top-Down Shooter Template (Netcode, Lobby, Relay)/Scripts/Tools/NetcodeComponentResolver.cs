using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//This component is made to prevent from showing "NetworkBehaviours require a NetworkObject" popup.
//It adds NetworkObject on select and removes it on deselect. 
//To fix issues just select object (click on it) and deselect it (click on a free space or another object). 
//Resolver will remove NetworkObject component on disable event.
//For more details check NetcodeComponentResolverEditor.cs

namespace HEAVYART.TopDownShooter.Netcode
{
    public class NetcodeComponentResolver : MonoBehaviour
    {
        private void Start()
        {
            if (GetComponent<NetworkObject>() != null)
            {
                string errorLog = $"{name} prefab saved incorrectly.\n"
                                + "Please turn off play mode, select prefab and deselect it.\n" +
                                  "Resolver will remove NetworkObject component on disable event.";

                Debug.LogError(errorLog, gameObject);
            }
        }

        void FixedUpdate() { }
    }
}
