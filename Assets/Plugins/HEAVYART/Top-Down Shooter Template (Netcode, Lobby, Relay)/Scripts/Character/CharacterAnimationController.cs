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
        private bool aimChest;
        private bool aimUpperChest;
        private bool aimBonesCaptured;
        private float visualYawOffset;
        private bool continuousLookYaw;

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
            BindAnimator(animator);

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
        }

        /// <summary>
        /// Retarget gameplay animation/IK to another humanoid Animator (e.g. customized body).
        /// </summary>
        public void RebindToAnimator(
            Animator newAnimator,
            bool allowProceduralAiming = true,
            float yawOffset = 0f,
            bool followLookContinuously = false)
        {
            if (newAnimator == null)
                return;

            BindAnimator(newAnimator, allowProceduralAiming, yawOffset, followLookContinuously);
        }

        private void BindAnimator(
            Animator target,
            bool allowProceduralAiming = true,
            float yawOffset = 0f,
            bool followLookContinuously = false)
        {
            animator = target;
            if (animator == null)
                return;

            visualYawOffset = yawOffset;
            continuousLookYaw = followLookContinuously;

            spine = allowProceduralAiming ? animator.GetBoneTransform(HumanBodyBones.Spine) : null;
            chest = null;
            upperChest = null;
            aimChest = false;
            aimUpperChest = false;
            aimBonesCaptured = true;

            iKController = animator.GetComponent<CharacterIKController>();
            if (iKController == null)
                iKController = animator.gameObject.AddComponent<CharacterIKController>();

            animatorLayerWeights = new float[animator.layerCount];
        }

        void LateUpdate()
        {
            if (healthController == null || healthController.isAlive == false)
                return;

            animator.SetFloat("Movement", 0);

            //Set movement animation speed
            var sprintMultiplier = playerBehaviour != null && playerBehaviour.IsSprinting
                ? playerBehaviour.SprintSpeedMultiplier
                : 1f;
            animator.SetFloat("MovementSpeedMultiplier", modifiersControlSystem.CalculateSpeedMultiplier() * sprintMultiplier);

            Quaternion targetRotation;

            var lookFlat = FlattenHorizontal(lineOfSightTransform.forward);

            if (movementSpeed > 0.01f) // If character moves
            {
                bool isOppositeDirections = Vector3.Dot(movementDirection, lookFlat) < 0;

                //Set movement direction
                animator.SetFloat("Movement", isOppositeDirections ? -1 : 1);

                targetRotation = Quaternion.LookRotation(movementDirection);

                //Rotate body in direction of aiming (a little bit). Fixes Quaternion.Slerp rotation in wrong direction.

                //Calculate additional angle (if character moves forward)
                float additionalLineOfSightAngle = Mathf.DeltaAngle(0, Quaternion.FromToRotation(movementDirection, lookFlat).eulerAngles.y);

                if (isOppositeDirections)
                {
                    //Calculate additional angle if character moves backwards
                    targetRotation *= Quaternion.Euler(0, -180, 0);
                    additionalLineOfSightAngle = Mathf.DeltaAngle(0, Quaternion.FromToRotation(movementDirection, -lookFlat).eulerAngles.y);
                }

                //Apply additional rotation
                float lineOfSightRotationFactor = continuousLookYaw ? 0.35f : 0.1f;
                targetRotation *= Quaternion.Euler(0, additionalLineOfSightAngle * lineOfSightRotationFactor, 0);
            }
            else // If it stands
            {
                targetRotation = continuousLookYaw
                    ? Quaternion.LookRotation(lookFlat)
                    : Quaternion.LookRotation(FindClosestDirection(lookFlat));
            }

            var yawOnly = Quaternion.Euler(0f, targetRotation.eulerAngles.y + visualYawOffset, 0f);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, yawOnly, rotationSmoothness * Time.deltaTime);

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

            //Handle layer switch smoothness (include base layer 0)
            for (int i = 0; i < animatorLayerWeights.Length; i++)
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

            if (animatorLayerWeights == null || animator == null)
                return;

            // Exclusive layer weights — having Pistol+Rifle movement both at 1 breaks CC arms.
            for (var i = 0; i < animatorLayerWeights.Length; i++)
                animatorLayerWeights[i] = 0;

            if (weaponGrip == WeaponGrip.Rifle)
            {
                if (animatorLayerWeights.Length > 1) animatorLayerWeights[1] = 1;
                if (animatorLayerWeights.Length > 3) animatorLayerWeights[3] = 1;
            }
            else if (weaponGrip == WeaponGrip.Pistol)
            {
                if (animatorLayerWeights.Length > 0) animatorLayerWeights[0] = 1;
                if (animatorLayerWeights.Length > 2) animatorLayerWeights[2] = 1;
            }

            for (var i = 0; i < animator.layerCount && i < animatorLayerWeights.Length; i++)
                animator.SetLayerWeight(i, animatorLayerWeights[i]);
        }

        public void SetTargetingTransform(Transform targetingTransform)
        {
            this.targetingTransform = targetingTransform;
        }

        private static Vector3 FlattenHorizontal(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
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
