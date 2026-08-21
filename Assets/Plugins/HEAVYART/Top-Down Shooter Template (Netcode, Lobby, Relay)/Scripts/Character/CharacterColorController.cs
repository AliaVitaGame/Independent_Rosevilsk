using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CharacterColorController : NetworkBehaviour
    {
        [Range(0, 1f)]
        public float colorFactor = 0.5f;
        public List<Renderer> renderers = new List<Renderer>();

        private CharacterIdentityControl identityControl;

        private void Awake()
        {
            identityControl = GetComponent<CharacterIdentityControl>();
        }

        public override void OnNetworkSpawn()
        {
            if (identityControl.isPlayer)
            {
                //Get color
                Color targetColor = identityControl.spawnParameters.Value.color;

                for (int i = 0; i < renderers.Count; i++)
                {
                    //Set color
                    renderers[i].material.color = Color.Lerp(renderers[i].material.color, targetColor, colorFactor);
                }
            }
        }
    }
}
