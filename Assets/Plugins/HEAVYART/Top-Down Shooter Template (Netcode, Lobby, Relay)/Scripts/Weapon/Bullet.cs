using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class Bullet : MonoBehaviour
    {
        public float bulletRadius = 0.25f;
        public float bulletLifetime = 1;
        public float lagCompensationFactor = 0.1f;

        private BulletParameters bulletParameters;
        private GameObject bulletTrail;

        private Vector3 previousPosition;
        private float bulletSpawnTime = 0;

        private bool isWaitingForDestroy = false;
        private bool isInitialized = false;

        public void Initialize(BulletParameters bulletParameters, Transform bulletSpawnPointTransform, Transform muzzleFlashPrefab)
        {
            transform.position = bulletSpawnPointTransform.position;
            previousPosition = bulletParameters.startPosition;

            this.bulletParameters = bulletParameters;

            //Create and destroy muzzle flash (with delay)
            Transform muzzleFlash = Instantiate(muzzleFlashPrefab, bulletSpawnPointTransform.position, Quaternion.LookRotation(bulletParameters.direction));
            muzzleFlash.parent = bulletSpawnPointTransform;
            Destroy(muzzleFlash.gameObject, 0.1f);

            //Find and deactivate bullet trail (activates at the end of frame)
            bulletTrail = transform.GetChild(0).gameObject;
            bulletTrail.SetActive(false);

            bulletSpawnTime = Time.time;
            isInitialized = true;
        }

        void FixedUpdate()
        {
            var networkManager = NetworkManager.Singleton;
            if (!isInitialized || networkManager == null || !networkManager.IsListening)
            {
                Destroy(gameObject);
                return;
            }

            double deltaTime = networkManager.ServerTime.Time - bulletParameters.startTime;

            if (deltaTime < 0) return;

            //Calculated bullet position ("server bullet")
            Vector3 serverBulletPosition = bulletParameters.startPosition + bulletParameters.direction * (float)(bulletParameters.speed * deltaTime);

            Vector3 bulletLocalMovementStep = bulletParameters.direction * bulletParameters.speed * Time.fixedDeltaTime;

            //Difference between bullet position on server and bullet position on client 
            Vector3 difference = serverBulletPosition - (transform.position + bulletLocalMovementStep);

            //Check if we're not in front of bullet's calculated position
            if (Vector3.Dot(bulletLocalMovementStep, difference) > 0)
            {
                Vector3 lagCompensation = difference * lagCompensationFactor;

                //Move closer to server bullet position
                transform.position += Vector3.ClampMagnitude(bulletLocalMovementStep + lagCompensation, bulletParameters.speed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.LookRotation(bulletParameters.direction);
            }

            //Difference between current and previous bullet position on server 
            Vector3 movementDelta = previousPosition - serverBulletPosition;

            float trackingDistance = Mathf.Max(movementDelta.magnitude, bulletParameters.speed * Time.fixedDeltaTime * 4);
            float raycastDistance = Mathf.Min(trackingDistance, bulletParameters.speed * (float)deltaTime);

            Vector3 startPoint = serverBulletPosition - bulletParameters.direction * raycastDistance;

            //Check bullet hit
            if (isWaitingForDestroy == false && Physics.SphereCast(startPoint, bulletRadius, bulletParameters.direction, out RaycastHit hit, raycastDistance))
            {
                bool allowToHandleHit = true;

                //Check if current player didn't hit itself. Doesn't suppose to, but it could be with high ping.
                NetworkObject sender = GameManager.Instance.userControl.FindCharacterByID(bulletParameters.senderID);
                if (sender != null && sender.transform == hit.transform) allowToHandleHit = false;

                if (allowToHandleHit == true)
                {
                    CommandReceiver commandReceiver = hit.transform.GetComponent<CommandReceiver>();

                    //Check if object is able to receive modifiers
                    if (commandReceiver != null)
                    {
                        //Receive hit modifiers (broadcast message)
                        if (networkManager.IsServer)
                            commandReceiver.ReceiveBulletHitRpc(bulletParameters.modifiers, bulletParameters.ownerID, networkManager.ServerTime.Time);

                        //Destroy bullet (cause it hits something)
                        StartCoroutine(RunBulletDestroy(hit.point));

                        isWaitingForDestroy = true;
                    }
                }
            }

            previousPosition = serverBulletPosition;

            //Activates bullet trail at the end of frame (it deactivates on spawn)
            //Here could be placed any other visual activation logic
            bulletTrail.gameObject.SetActive(true);

            //Destroy bullets by timeout
            if (Time.time > bulletSpawnTime + bulletLifetime)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator RunBulletDestroy(Vector3 hitPoint)
        {
            //For better look we can wait for a few frames, to make ammo reach it's hit point.
            //It keeps flying while we wait.
            float delay = (transform.position - hitPoint).magnitude / bulletParameters.speed;
            yield return new WaitForSeconds(delay - Time.fixedDeltaTime);

            //Destroy bullet
            bulletTrail.SetActive(false);
            Destroy(gameObject);
        }
    }
}
