using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.TargetHints
{
    /// <summary>
    /// World-side marker that UI arrows can track.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetHintTarget : MonoBehaviour
    {
        private static readonly List<TargetHintTarget> ActiveTargets = new();

        [SerializeField] private Vector3 _worldOffset = new(0f, 2f, 0f);
        [SerializeField] private bool _isHintEnabled = true;
        [SerializeField] private TargetHintKind _kind = TargetHintKind.Passenger;

        public static IReadOnlyList<TargetHintTarget> All => ActiveTargets;

        public Vector3 WorldPosition => transform.position + _worldOffset;
        public bool IsHintEnabled => _isHintEnabled && isActiveAndEnabled;
        public TargetHintKind Kind => _kind;

        public event Action<TargetHintTarget> HintEnabledChanged;

        private void OnEnable()
        {
            if (!ActiveTargets.Contains(this))
                ActiveTargets.Add(this);
        }

        private void OnDisable()
        {
            ActiveTargets.Remove(this);
        }

        public void SetKind(TargetHintKind kind)
        {
            _kind = kind;
        }

        public void SetHintEnabled(bool enabled)
        {
            if (_isHintEnabled == enabled)
                return;

            _isHintEnabled = enabled;
            HintEnabledChanged?.Invoke(this);
        }
    }
}
