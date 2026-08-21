using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CharacterEffectsController : NetworkBehaviour
    {
        public float delayBeforeDrop = 1;
        public float delayBeforeStartMoving = 2;
        public float moveDownTime = 2;

        public void RunDestroyScenario(bool stopCameraMovement)
        {
            StartCoroutine(ProcessCharacterDestroySteps(stopCameraMovement));
        }

        IEnumerator ProcessCharacterDestroySteps(bool stopCameraMovement)
        {
            //It's just a bunch of required steps to destroy character properly, with all the delays, etc.

            //Remove it from list of characters
            GameManager.Instance.userControl.RemoveNetworkObject(GetComponent<NetworkObject>());

            //Disable collision. Character would be on scene for few seconds more, but it wouldn't stop bullets, bots or other players.
            GetComponent<CapsuleCollider>().enabled = false;

            //Drop something (could be nothing)
            yield return new WaitForSeconds(delayBeforeDrop);
            GetComponent<DropController>().Drop();

            //Let animation play till the end and (maybe) wait a little bit more
            yield return new WaitForSeconds(delayBeforeStartMoving);

            //We should stop camera movement before player starts moving under ground (in case if it's our player)
            if (stopCameraMovement == true)
                if (GameManager.Instance.userControl == null)
                    Camera.main.GetComponent<GameCameraController>().StopCameraMovement();

            //Disappearing under ground (here could be any other effect)
            while (moveDownTime > 0)
            {
                if (IsOwner == true)
                    transform.position += Vector3.down * Time.deltaTime;

                moveDownTime -= Time.deltaTime;
                yield return 0;
            }

            if (IsServer == true)
            {
                //Destroy as network object (only server allowed to do it)
                NetworkObject.Despawn();
            }
        }
    }
}
