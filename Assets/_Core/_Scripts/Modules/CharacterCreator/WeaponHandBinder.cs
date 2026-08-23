using UnityEngine;

namespace Modules.CharacterCreator
{
    /// <summary>
    /// Keeps a weapon root (still parented under Kyle's hand for lifetime safety)
    /// glued to the customized humanoid hand after pose relay.
    /// </summary>
    [DefaultExecutionOrder(10001)]
    public sealed class WeaponHandBinder : MonoBehaviour
    {
        private Transform _targetHand;
        private Vector3 _localPosition;
        private Quaternion _localRotation;
        private bool _ready;

        public void Initialize(Transform targetHand, Vector3 localPosition, Quaternion localRotation)
        {
            _targetHand = targetHand;
            _localPosition = localPosition;
            _localRotation = localRotation;
            _ready = _targetHand != null;
        }

        private void LateUpdate()
        {
            if (!_ready || _targetHand == null)
                return;

            transform.SetPositionAndRotation(
                _targetHand.TransformPoint(_localPosition),
                _targetHand.rotation * _localRotation);
        }
    }
}
