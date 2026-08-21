using HEAVYART.TopDownShooter.Netcode;
using UnityEditor;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [RequireComponent(typeof(Animator))]

    public class CharacterIKController : MonoBehaviour
    {
        private Animator animator;
        private Transform leftHandGripTransform;

        private HealthController healthController;

        void Start()
        {
            animator = GetComponent<Animator>();
            healthController = transform.root.GetComponent<HealthController>();
        }

        public void UpdateLeftHandGripTransform(Transform updatedGripTransform)
        {
            leftHandGripTransform = updatedGripTransform;
        }

        void OnAnimatorIK()
        {
            if (healthController.isAlive == false)
                return;

            // Set the left hand target position and rotation, if one has been assigned
            if (leftHandGripTransform != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);

                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripTransform.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripTransform.rotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            }
        }
    }
}
