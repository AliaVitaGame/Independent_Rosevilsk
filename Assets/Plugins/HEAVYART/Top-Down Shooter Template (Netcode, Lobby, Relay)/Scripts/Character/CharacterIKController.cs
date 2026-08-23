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
            EnsureRefs();
        }

        public void UpdateLeftHandGripTransform(Transform updatedGripTransform)
        {
            leftHandGripTransform = updatedGripTransform;
        }

        void OnAnimatorIK()
        {
            EnsureRefs();

            if (healthController == null || animator == null)
                return;

            if (healthController.isAlive == false)
                return;

            // Unity fake-null: destroyed transforms still fail `!= null` with System.Object compare.
            if (leftHandGripTransform)
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

        private void EnsureRefs()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (healthController == null)
                healthController = transform.root.GetComponent<HealthController>();
        }
    }
}
