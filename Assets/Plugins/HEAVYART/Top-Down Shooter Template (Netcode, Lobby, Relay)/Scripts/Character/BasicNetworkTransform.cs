using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class BasicNetworkTransform : NetworkBehaviour
    {
        //Masks
        //Full synchronization will be enabled, in case if whole block is marked as true. See details below.

        //Position block
        public bool xPosition = true;
        public bool yPosition = true;
        public bool zPosition = true;

        //Rotation block
        [Space()]
        public bool xRotation = true;
        public bool yRotation = true;
        public bool zRotation = true;

        [Space()]
        //Regular position smoothness
        public int positionSmoothingFrames = 5;

        //Prediction force, based on previous and current positions
        public float positionExtrapolationFactor = 1f;

        [Space()]
        //Rotation smoothness (no prediction here)
        public float rotationInterpolationFactor = 0.5f;

        //Separated variables for partial synchronization
        private NetworkVariable<float> xPositionValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> yPositionValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> zPositionValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> xRotationValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> yRotationValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> zRotationValue = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);

        //Variables for full synchronization
        private NetworkVariable<Vector3> fullPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> fullRotation = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner);

        private float teleportDistance = 1;
        private Vector3 previousPosition;
        private Vector3 extrapolationOffset;
        private Vector3 positionDampVelocity;

        public override void OnNetworkSpawn()
        {
            //Synchronize position on spawn
            if (IsOwner)
                fullPosition.Value = transform.localPosition;

            //Local position automatically transforms to global, it there is no parent object. Same with local rotation.
            transform.localPosition = fullPosition.Value;
            previousPosition = transform.localPosition;
        }

        private void FixedUpdate()
        {
            if (IsOwner) //Write 
            {

                if (xPosition && yPosition && zPosition)
                {
                    fullPosition.Value = transform.localPosition;
                }
                else
                {
                    Vector3 position = transform.localPosition;
                    if (xPosition) xPositionValue.Value = position.x;
                    if (yPosition) yPositionValue.Value = position.y;
                    if (zPosition) zPositionValue.Value = position.z;
                }

                if (xRotation && yRotation && zRotation)
                {
                    fullRotation.Value = transform.localRotation;
                }
                else
                {
                    Vector3 rotation = transform.localRotation.eulerAngles;
                    if (xRotation) xRotationValue.Value = rotation.x;
                    if (yRotation) yRotationValue.Value = rotation.y;
                    if (zRotation) zRotationValue.Value = rotation.z;
                }
            }
            else //Read
            {

                Vector3 position = transform.localPosition;
                if (xPosition && yPosition && zPosition)
                {
                    position = fullPosition.Value;
                }
                else
                {
                    if (xPosition) position.x = xPositionValue.Value;
                    if (yPosition) position.y = yPositionValue.Value;
                    if (zPosition) position.z = zPositionValue.Value;
                }

                Quaternion rotation = transform.localRotation;
                if (xRotation && yRotation && zRotation)
                {
                    rotation = fullRotation.Value;
                }
                else
                {
                    Vector3 eulerAngles = Vector3.zero;
                    if (xRotation) eulerAngles.x = xRotationValue.Value;
                    if (yRotation) eulerAngles.y = yRotationValue.Value;
                    if (zRotation) eulerAngles.z = zRotationValue.Value;

                    rotation = Quaternion.Euler(eulerAngles);
                }

                //Teleport if we too far from required position
                if ((transform.localPosition - position).magnitude > teleportDistance)
                    transform.localPosition = position;

                //Apply
                extrapolationOffset = (position - previousPosition) * positionExtrapolationFactor * positionSmoothingFrames;
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, position + extrapolationOffset, ref positionDampVelocity, positionSmoothingFrames * Time.fixedDeltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, rotation, rotationInterpolationFactor);

                previousPosition = position;
            }
        }
    }
}
