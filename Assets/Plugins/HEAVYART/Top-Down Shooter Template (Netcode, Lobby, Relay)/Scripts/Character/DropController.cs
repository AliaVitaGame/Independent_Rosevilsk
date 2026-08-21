using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class DropController : NetworkBehaviour
    {
        private List<PickUpItemController> dropElements = new List<PickUpItemController>();
        private float dropChance;

        private CharacterIdentityControl identityControl;

        private void Start()
        {
            identityControl = GetComponent<CharacterIdentityControl>();

            if (identityControl.isPlayer == true) //Set player drop settings
            {
                int modelIndex = identityControl.spawnParameters.Value.modelIndex;
                dropElements = SettingsManager.Instance.player.configs[modelIndex].dropElements;
                dropChance = SettingsManager.Instance.player.configs[modelIndex].dropChance;
            }
            else //Set bot drop settings
            {
                int modelIndex = identityControl.spawnParameters.Value.modelIndex;
                dropElements = SettingsManager.Instance.ai.configs[modelIndex].dropElements;
                dropChance = SettingsManager.Instance.ai.configs[modelIndex].dropChance;
            }
        }

        public void Drop()
        {
            if (IsServer)
            {
                if (dropChance > Random.Range(0, 100))
                {
                    int elementID = Random.Range(0, dropElements.Count);
                    Vector3 offset = Vector3.up * 0.5f;

                    //Spawn random drop element
                    GameObject dropGameObject = Instantiate(dropElements[elementID].gameObject, transform.position + offset, Quaternion.identity);
                    dropGameObject.GetComponent<NetworkObject>().Spawn(true);
                }
            }
        }
    }
}
