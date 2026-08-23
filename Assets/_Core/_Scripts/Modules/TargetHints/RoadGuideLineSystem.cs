using System.Collections.Generic;
using Game.Core.Vehicles;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;

namespace Modules.TargetHints
{
    /// <summary>
    /// Draws colored road paths toward every enabled hint target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadGuideLineSystem : MonoBehaviour
    {
        [SerializeField] private float _heightOffset = 0.08f;
        [SerializeField] private float _lineWidth = 0.4f;
        [SerializeField] private float _repathInterval = 0.35f;
        [SerializeField] private float _repathMoveThreshold = 6f;
        [SerializeField] private Color _vehicleColor = new(1f, 0.82f, 0.18f, 0.9f);
        [SerializeField] private Color _passengerColor = new(1f, 0.15f, 0.12f, 0.9f);
        [SerializeField] private Color _exitColor = new(0.2f, 0.92f, 0.42f, 0.9f);

        private readonly RoadPathGraph _graph = new();
        private readonly List<TargetHintTarget> _enabledTargets = new();
        private readonly List<LineRenderer> _lines = new();
        private readonly List<Vector3> _pathBuffer = new();

        private DriveableVehicleInteraction _vehicle;
        private Transform _player;
        private Vector3 _lastOrigin;
        private float _nextRepathTime;
        private Material _lineMaterial;

        private void Start()
        {
            _vehicle = FindFirstObjectByType<DriveableVehicleInteraction>();
            _graph.Rebuild();
            _nextRepathTime = 0f;
        }

        private void LateUpdate()
        {
            var origin = ResolveOrigin();
            CollectEnabledTargets(origin);
            EnsureLineCount(_enabledTargets.Count);
            var movedFar = (origin - _lastOrigin).sqrMagnitude >= _repathMoveThreshold * _repathMoveThreshold;
            var due = Time.unscaledTime >= _nextRepathTime;
            if (due || movedFar)
            {
                _lastOrigin = origin;
                _nextRepathTime = Time.unscaledTime + _repathInterval;
            }

            for (var i = 0; i < _enabledTargets.Count; i++)
            {
                var line = _lines[i];
                if (!line.gameObject.activeSelf)
                    line.gameObject.SetActive(true);

                ApplyColor(line, _enabledTargets[i].Kind);
                line.startWidth = _lineWidth;
                line.endWidth = _lineWidth;

                if (due || movedFar)
                    UpdatePath(line, origin, _enabledTargets[i].transform.position);
            }

            for (var i = _enabledTargets.Count; i < _lines.Count; i++)
            {
                if (_lines[i].gameObject.activeSelf)
                    _lines[i].gameObject.SetActive(false);
            }
        }

        private void CollectEnabledTargets(Vector3 origin)
        {
            _enabledTargets.Clear();
            TargetHintTarget nearestVehicle = null;
            TargetHintTarget nearestPassenger = null;
            TargetHintTarget nearestExit = null;
            var bestVehicle = float.MaxValue;
            var bestPassenger = float.MaxValue;
            var bestExit = float.MaxValue;

            var all = TargetHintTarget.All;
            for (var i = 0; i < all.Count; i++)
            {
                var target = all[i];
                if (target == null || !target.IsHintEnabled)
                    continue;

                var offset = target.transform.position - origin;
                offset.y = 0f;
                var sqr = offset.sqrMagnitude;

                switch (target.Kind)
                {
                    case TargetHintKind.Vehicle:
                        if (sqr < bestVehicle)
                        {
                            bestVehicle = sqr;
                            nearestVehicle = target;
                        }
                        break;
                    case TargetHintKind.Exit:
                        if (sqr < bestExit)
                        {
                            bestExit = sqr;
                            nearestExit = target;
                        }
                        break;
                    default:
                        if (sqr < bestPassenger)
                        {
                            bestPassenger = sqr;
                            nearestPassenger = target;
                        }
                        break;
                }
            }

            if (nearestVehicle != null)
                _enabledTargets.Add(nearestVehicle);
            if (nearestPassenger != null)
                _enabledTargets.Add(nearestPassenger);
            if (nearestExit != null)
                _enabledTargets.Add(nearestExit);
        }

        private Vector3 ResolveOrigin()
        {
            if (_vehicle != null && _vehicle.IsDriving)
                return _vehicle.transform.position;

            if (_player == null)
                _player = FindPlayer();

            if (_player != null)
                return _player.position;

            if (_vehicle != null)
                return _vehicle.transform.position;

            return transform.position;
        }

        private static Transform FindPlayer()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.userControl == null)
                return null;

            var localPlayer = gameManager.userControl.localPlayer;
            return localPlayer != null ? localPlayer.transform : null;
        }

        private void UpdatePath(LineRenderer line, Vector3 from, Vector3 to)
        {
            if (_graph.TryFindPath(from, to, _pathBuffer, _heightOffset))
            {
                line.enabled = true;
                line.positionCount = _pathBuffer.Count;
                for (var p = 0; p < _pathBuffer.Count; p++)
                    line.SetPosition(p, _pathBuffer[p]);
                return;
            }

            line.positionCount = 0;
            line.enabled = false;
        }

        private void EnsureLineCount(int count)
        {
            while (_lines.Count < count)
                _lines.Add(CreateLine($"RoadGuideLine_{_lines.Count + 1}"));
        }

        private LineRenderer CreateLine(string objectName)
        {
            var go = new GameObject(objectName, typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = GetLineMaterial();
            line.widthMultiplier = 1f;
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            line.positionCount = 0;
            return line;
        }

        private Material GetLineMaterial()
        {
            if (_lineMaterial != null)
                return _lineMaterial;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            _lineMaterial = new Material(shader)
            {
                name = "RoadGuideLine",
                color = Color.white
            };
            _lineMaterial.EnableKeyword("_EMISSION");
            return _lineMaterial;
        }

        private void ApplyColor(LineRenderer line, TargetHintKind kind)
        {
            var color = kind switch
            {
                TargetHintKind.Vehicle => _vehicleColor,
                TargetHintKind.Exit => _exitColor,
                _ => _passengerColor
            };

            line.startColor = color;
            line.endColor = color;
        }
    }
}
