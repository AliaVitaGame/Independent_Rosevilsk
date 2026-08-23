using System.Collections.Generic;
using UnityEngine;

namespace Modules.TargetHints
{
    /// <summary>
    /// Instantiates one screen arrow per enabled world hint target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetHintArrowPool : MonoBehaviour
    {
        [SerializeField] private TargetHintArrow _template;
        [SerializeField] private Transform _arrowsRoot;

        private readonly List<TargetHintArrow> _arrows = new();
        private readonly List<TargetHintTarget> _enabledTargets = new();

        private void Awake()
        {
            if (_template == null)
                _template = GetComponent<TargetHintArrow>();

            if (_template == null)
                _template = FindFirstObjectByType<TargetHintArrow>(FindObjectsInactive.Include);

            if (_arrowsRoot == null && _template != null)
                _arrowsRoot = _template.transform.parent;

            if (_template != null)
                _template.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_template == null)
                return;

            CollectEnabledTargets();
            EnsureArrowCount(_enabledTargets.Count);

            for (var i = 0; i < _enabledTargets.Count; i++)
            {
                var arrow = _arrows[i];
                if (!arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(true);

                if (arrow.Target != _enabledTargets[i])
                    arrow.SetTarget(_enabledTargets[i]);
            }

            for (var i = _enabledTargets.Count; i < _arrows.Count; i++)
            {
                if (_arrows[i].gameObject.activeSelf)
                    _arrows[i].gameObject.SetActive(false);
            }
        }

        private void CollectEnabledTargets()
        {
            _enabledTargets.Clear();
            var all = TargetHintTarget.All;
            for (var i = 0; i < all.Count; i++)
            {
                var target = all[i];
                if (target != null && target.IsHintEnabled)
                    _enabledTargets.Add(target);
            }
        }

        private void EnsureArrowCount(int count)
        {
            while (_arrows.Count < count)
            {
                var instance = Instantiate(_template, _arrowsRoot);
                instance.name = $"TargetHintArrow_{_arrows.Count + 1}";
                instance.gameObject.SetActive(true);
                _arrows.Add(instance);
            }
        }
    }
}
