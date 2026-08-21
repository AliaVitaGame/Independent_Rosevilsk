using System.Collections.Generic;
using UnityEngine;
using System;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class CharacterAnimationController : MonoBehaviour
    {
        public Animator animator;

        [Space]
        public float spineWeight = 0.1f;
        public float chestWeight = 0.3f;
        public float upperChestWeight = 0.6f;

        [Space]
        public float rotationSmoothness = 5;
        public float layerSwitchSmoothness = 10f;

        private Transform targetingTransform;
        private Transform lineOfSightTransform;

        private Transform spine;
        private Transform chest;
        private Transform upperChest;

        private Vector3 movementDirection;
        private float movementSpeed;
        private Vector3 previousPosition;
        private List<Vector3> directions;

        private HealthController healthController;
        private ModifiersControlSystem modifiersControlSystem;
        private CharacterIKController iKController;
        private PlayerBehaviour playerBehaviour;

        private float[] animatorLayerWeights;

        void Awake()
        {
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);

            lineOfSightTransform = transform.root.GetComponent<WeaponControlSystem>().lineOfSightTransform;

            previousPosition = transform.position;
            movementDirection = Vector3.forward;
            movementSpeed = 0;

            //Available body directions (for idle pose)
            directions = new List<Vector3>() { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
            playerBehaviour = GetComponent<PlayerBehaviour>();
            healthController = GetComponent<HealthController>();
            healthController.OnDeath += PlayDeathAnimation;

            iKController = animator.transform.GetComponent<CharacterIKController>();

            animatorLayerWeights = new float[animator.layerCount];
        }

        void LateUpdate()
        {
            if (healthController.isAlive == false)
                return;

            animator.SetFloat("Movement", 0);

            //Set movement animation speed
            var sprintMultiplier = playerBehaviour != null && playerBehaviour.IsSprinting
                ? playerBehaviour.SprintSpeedMultiplier
                : 1f;
            animator.SetFloat("MovementSpeedMultiplier", modifiersControlSystem.CalculateSpeedMultiplier() * sprintMultiplier);

            Quaternion targetRotation;

            if (movementSpeed > 0.01f) // If character moves
            {
                bool isOppositeDirections = Vector3.Dot(movementDirection, lineOfSightTransform.forward) < 0;

                //Set movement direction
                animator.SetFloat("Movement", isOppositeDirections ? -1 : 1);

                targetRotation = Quaternion.LookRotation(movementDirection);

                //Rotate body in direction of aiming (a little bit). Fixes Quaternion.Slerp rotation in wrong direction.

                //Calculate additional angle (if character moves forward)
                float additionalLineOfSightAngle = Mathf.DeltaAngle(0, Quaternion.FromToRotation(movementDirection, lineOfSightTransform.forward).eulerAngles.y);

                if (isOppositeDirections)
                {
                    //Calculate additional angle if character moves backwards
                    targetRotation *= Quaternion.Euler(0, -180, 0);
                    additionalLineOfSightAngle = Mathf.DeltaAngle(0, Quaternion.FromToRotation(movementDirection, -lineOfSightTransform.forward).eulerAngles.y);
                }

                //Apply additional rotation
                float lineOfSightRotationFactor = 0.1f;
                targetRotation *= Quaternion.Euler(0, additionalLineOfSightAngle * lineOfSightRotationFactor, 0);
            }
            else // If it stands
            {
                //Rotate body (legs) to closest direction
                targetRotation = Quaternion.LookRotation(FindClosestDirection(lineOfSightTransform.forward));
            }

            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);

            //Rotate skeleton parts
            HandleAiming(spine, spineWeight);
            HandleAiming(chest, chestWeight);
            HandleAiming(upperChest, upperChestWeight);
        }

        private void HandleAiming(Transform bone, float weight)
        {
            if (bone == null)
                return;

            //Upper body will be pointed in the line of sight. Weapon will be pointed at targetingTransform.
            Vector3 horizontalLineOfSight = targetingTransform.position - lineOfSightTransform.position;
            horizontalLineOfSight.Normalize();

            Quaternion boneRotation = Quaternion.FromToRotation(animator.transform.forward, horizontalLineOfSight);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, boneRotation, weight) * bone.rotation;
        }

        void FixedUpdate()
        {
            //Calculate movement speed and direction for further using in animation algorithms

            var movementDelta = transform.position - previousPosition;
            movementDelta.y = 0;

            //Set movement speed
            movementSpeed = movementDelta.magnitude;

            if (movementSpeed > 0.01f)
            {
                //Set movement direction 
                movementDirection = movementDelta.normalized;
            }

            previousPosition = transform.position;

            //Handle layer switch smoothness
            for (int i = 1; i < animatorLayerWeights.Length; i++)
            {
                float weight = Mathf.MoveTowards(animator.GetLayerWeight(i), animatorLayerWeights[i], layerSwitchSmoothness * Time.fixedDeltaTime);
                animator.SetLayerWeight(i, weight);
            }
        }

        public void PlayFireAnimation()
        {
            //Play fire animation
            animator.SetTrigger("Fire");
        }

        public void PlayDeathAnimation()
        {
            for (int i = 1; i < animatorLayerWeights.Length; i++)
            {
                animatorLayerWeights[i] = 0;
                animator.SetLayerWeight(i, 0);
            }

            //Play death animation
            animator.SetTrigger("Death");
        }

        public void PlayJumpAnimation()
        {
            animator.SetTrigger("Jump");
        }

        public void UpdateWeaponGrip(WeaponGrip weaponGrip, Transform leftHandGripIKTransform)
        {
            //Handle IK
            if (iKController != null)
            {
                //Apply IK parameters for left hand
                iKController.UpdateLeftHandGripTransform(leftHandGripIKTransform);
            }

            for (int i = 1; i < animatorLayerWeights.Length; i++)
            {
                animatorLayerWeights[i] = 0;
            }

            //Turn on required layer
            if (weaponGrip == WeaponGrip.Rifle)
            {
                animatorLayerWeights[1] = 1;
                animatorLayerWeights[3] = 1;
            }

            //Turn on required layer
            if (weaponGrip == WeaponGrip.Pistol)
            {
                animatorLayerWeights[0] = 1;
                animatorLayerWeights[2] = 1;
            }
        }

        public void SetTargetingTransform(Transform targetingTransform)
        {
            this.targetingTransform = targetingTransform;
        }

        private Vector3 FindClosestDirection(Vector3 directionToCompareWith)
        {
            //Find closest direction, to use it when character stands

            Vector3 closestDirection = directions[0];
            float closestDotProduct = Vector3.Dot(directions[0], directionToCompareWith);

            for (int i = 1; i < directions.Count; i++)
            {
                //Returns values between (approximately) -1 and 1. The closer two directions the bigger result value.
                float dotProduct = Vector3.Dot(directions[i], directionToCompareWith);

                //Find the biggest one (closest direction)
                if (dotProduct > closestDotProduct)
                {
                    closestDirection = directions[i];
                    closestDotProduct = dotProduct;
                }
            }

            return closestDirection;
        }
    }
}
