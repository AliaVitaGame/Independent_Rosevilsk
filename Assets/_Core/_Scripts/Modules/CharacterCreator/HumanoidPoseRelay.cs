using UnityEngine;

namespace Modules.CharacterCreator
{
    /// <summary>
    /// Copies Kyle arm/leg muscles onto CC after animation/IK.
    /// Spine/chest muscles and bodyRotation are kept from CC rest pose — Kyle's
    /// Ribs-as-Spine mapping twists a full humanoid waist if those are copied.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class HumanoidPoseRelay : MonoBehaviour
    {
        private Animator _source;
        private Animator _target;
        private HumanPoseHandler _sourceHandler;
        private HumanPoseHandler _targetHandler;
        private HumanPose _srcPose;
        private HumanPose _dstPose;
        private float[] _restMuscles;
        private float[] _workMuscles;
        private Quaternion _restBodyRotation;
        private float _restBodyY;
        private bool[] _copyMuscle;
        private bool _ready;

        public void Initialize(Animator source, Animator target)
        {
            DisposeHandlers();
            _source = source;
            _target = target;
            _ready = false;

            if (!IsUsableHumanoid(_source) || !IsUsableHumanoid(_target))
            {
                Debug.LogError("[Appearance] Pose relay needs valid humanoid Avatars on Kyle and the custom body.");
                return;
            }

            _target.runtimeAnimatorController = null;
            _target.applyRootMotion = false;
            _target.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _target.enabled = true;
            _target.Rebind();
            _target.Update(0f);
            _target.enabled = false;

            _sourceHandler = new HumanPoseHandler(_source.avatar, _source.transform);
            _targetHandler = new HumanPoseHandler(_target.avatar, _target.transform);

            _targetHandler.GetHumanPose(ref _dstPose);
            _restMuscles = (float[])_dstPose.muscles.Clone();
            _workMuscles = new float[_restMuscles.Length];
            _restBodyRotation = _dstPose.bodyRotation;
            _restBodyY = _dstPose.bodyPosition.y;

            var muscleCount = Mathf.Min(_restMuscles.Length, HumanTrait.MuscleCount);
            _copyMuscle = new bool[muscleCount];
            for (var i = 0; i < muscleCount; i++)
                _copyMuscle[i] = !IsTorsoMuscle(HumanTrait.MuscleName[i]);

            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready || _source == null || _target == null)
                return;

            _sourceHandler.GetHumanPose(ref _srcPose);

            System.Array.Copy(_restMuscles, _workMuscles, _workMuscles.Length);
            var n = Mathf.Min(_workMuscles.Length, _srcPose.muscles.Length, _copyMuscle.Length);
            for (var i = 0; i < n; i++)
            {
                if (_copyMuscle[i])
                    _workMuscles[i] = _srcPose.muscles[i];
            }

            _dstPose.muscles = _workMuscles;
            _dstPose.bodyRotation = _restBodyRotation;
            _dstPose.bodyPosition = new Vector3(
                _srcPose.bodyPosition.x,
                _restBodyY,
                _srcPose.bodyPosition.z);

            _targetHandler.SetHumanPose(ref _dstPose);
        }

        private static bool IsTorsoMuscle(string name)
        {
            return name.IndexOf("Spine", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnDestroy()
        {
            DisposeHandlers();
        }

        private void DisposeHandlers()
        {
            _sourceHandler?.Dispose();
            _targetHandler?.Dispose();
            _sourceHandler = null;
            _targetHandler = null;
            _ready = false;
        }

        private static bool IsUsableHumanoid(Animator animator)
        {
            return animator != null
                   && animator.avatar != null
                   && animator.avatar.isValid
                   && animator.avatar.isHuman;
        }
    }
}
