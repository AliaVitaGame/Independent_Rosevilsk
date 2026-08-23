using System.Collections.Generic;
using FCG;
using UnityEngine;

namespace Modules.TargetHints
{
    /// <summary>
    /// Undirected road graph from Fantastic City Generator waypoints.
    /// </summary>
    public sealed class RoadPathGraph
    {
        private const float EndpointLinkDistance = 32f;

        private readonly List<Vector3> _positions = new();
        private readonly List<List<int>> _edges = new();
        private readonly List<int> _open = new();
        private readonly List<float> _openScore = new();
        private readonly List<int> _cameFrom = new();
        private readonly List<float> _gScore = new();
        private readonly List<byte> _closed = new();
        private readonly List<int> _reverseBuffer = new();

        public int NodeCount => _positions.Count;
        public bool IsReady => _positions.Count > 1;

        public void Rebuild()
        {
            _positions.Clear();
            _edges.Clear();

            var traffic = Object.FindAnyObjectByType<TrafficSystem>();
            traffic?.UpdateAllWayPoints();

            var containers = Object.FindObjectsByType<FCGWaypointsContainer>(FindObjectsInactive.Exclude);
            var firstIndex = new int[containers.Length];
            var counts = new int[containers.Length];

            for (var c = 0; c < containers.Length; c++)
            {
                var container = containers[c];
                if (container.waypoints == null || container.waypoints.Count == 0)
                    container.GetWaypoints();

                firstIndex[c] = _positions.Count;
                var points = container.waypoints;
                if (points == null)
                {
                    counts[c] = 0;
                    continue;
                }

                var added = 0;
                for (var i = 0; i < points.Count; i++)
                {
                    if (points[i] == null)
                        continue;

                    AddNode(points[i].position);
                    if (added > 0)
                        AddUndirectedEdge(_positions.Count - 2, _positions.Count - 1);

                    added++;
                }

                counts[c] = added;
            }

            for (var a = 0; a < containers.Length; a++)
            {
                if (counts[a] == 0)
                    continue;

                for (var b = a + 1; b < containers.Length; b++)
                {
                    if (counts[b] == 0)
                        continue;

                    TryLink(firstIndex[a], firstIndex[b], EndpointLinkDistance);
                    TryLink(firstIndex[a], firstIndex[b] + counts[b] - 1, EndpointLinkDistance);
                    TryLink(firstIndex[a] + counts[a] - 1, firstIndex[b], EndpointLinkDistance);
                    TryLink(firstIndex[a] + counts[a] - 1, firstIndex[b] + counts[b] - 1, EndpointLinkDistance);
                }
            }

            ConnectDeclaredNextWays(containers, firstIndex, counts);
        }

        public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, float heightOffset)
        {
            path.Clear();
            if (!IsReady)
                return false;

            var start = FindNearest(from);
            var goal = FindNearest(to);
            if (start < 0 || goal < 0)
                return false;

            if (!TryAStar(start, goal))
                return false;

            for (var i = _reverseBuffer.Count - 1; i >= 0; i--)
                path.Add(WithHeight(_positions[_reverseBuffer[i]], heightOffset));

            Deduplicate(path);
            return path.Count >= 2;
        }

        private void ConnectDeclaredNextWays(
            FCGWaypointsContainer[] containers,
            int[] firstIndex,
            int[] counts)
        {
            var lookup = new Dictionary<FCGWaypointsContainer, int>(containers.Length);
            for (var i = 0; i < containers.Length; i++)
                lookup[containers[i]] = i;

            for (var i = 0; i < containers.Length; i++)
            {
                if (counts[i] == 0)
                    continue;

                LinkNextArray(containers[i].nextWay0, i, firstIndex, counts, lookup);
                LinkNextArray(containers[i].nextWay1, i, firstIndex, counts, lookup);
            }
        }

        private void LinkNextArray(
            FCGWaypointsContainer[] nexts,
            int from,
            int[] firstIndex,
            int[] counts,
            Dictionary<FCGWaypointsContainer, int> lookup)
        {
            if (nexts == null)
                return;

            for (var n = 0; n < nexts.Length; n++)
            {
                var next = nexts[n];
                if (next == null || !lookup.TryGetValue(next, out var to) || counts[to] == 0)
                    continue;

                TryLink(firstIndex[from], firstIndex[to], 48f);
                TryLink(firstIndex[from], firstIndex[to] + counts[to] - 1, 48f);
                TryLink(firstIndex[from] + counts[from] - 1, firstIndex[to], 48f);
                TryLink(firstIndex[from] + counts[from] - 1, firstIndex[to] + counts[to] - 1, 48f);
            }
        }

        private void AddNode(Vector3 position)
        {
            _positions.Add(position);
            _edges.Add(new List<int>(2));
        }

        private void AddUndirectedEdge(int a, int b)
        {
            if (a == b || a < 0 || b < 0)
                return;

            if (!_edges[a].Contains(b))
                _edges[a].Add(b);

            if (!_edges[b].Contains(a))
                _edges[b].Add(a);
        }

        private void TryLink(int a, int b, float maxDistance)
        {
            if (( _positions[a] - _positions[b]).sqrMagnitude <= maxDistance * maxDistance)
                AddUndirectedEdge(a, b);
        }

        private int FindNearest(Vector3 point)
        {
            var best = -1;
            var bestSqr = float.MaxValue;
            var sample = new Vector3(point.x, 0f, point.z);
            for (var i = 0; i < _positions.Count; i++)
            {
                var p = _positions[i];
                var delta = new Vector3(p.x - sample.x, 0f, p.z - sample.z);
                var sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = i;
            }

            return best;
        }

        private bool TryAStar(int start, int goal)
        {
            var count = _positions.Count;
            EnsureSearchBuffers(count);

            for (var i = 0; i < count; i++)
            {
                _gScore[i] = float.PositiveInfinity;
                _cameFrom[i] = -1;
                _closed[i] = 0;
            }

            _open.Clear();
            _openScore.Clear();
            _gScore[start] = 0f;
            PushOpen(start, Heuristic(start, goal));

            while (_open.Count > 0)
            {
                var current = PopOpen();
                if (_closed[current] != 0)
                    continue;

                if (current == goal)
                {
                    Reconstruct(current);
                    return true;
                }

                _closed[current] = 1;
                var neighbours = _edges[current];
                for (var i = 0; i < neighbours.Count; i++)
                {
                    var next = neighbours[i];
                    if (_closed[next] != 0)
                        continue;

                    var tentative = _gScore[current] + Vector3.Distance(_positions[current], _positions[next]);
                    if (tentative >= _gScore[next])
                        continue;

                    _cameFrom[next] = current;
                    _gScore[next] = tentative;
                    PushOpen(next, tentative + Heuristic(next, goal));
                }
            }

            return false;
        }

        private void Reconstruct(int current)
        {
            _reverseBuffer.Clear();
            while (current >= 0)
            {
                _reverseBuffer.Add(current);
                current = _cameFrom[current];
            }
        }

        private float Heuristic(int a, int b)
        {
            return Vector3.Distance(_positions[a], _positions[b]);
        }

        private void PushOpen(int node, float score)
        {
            _open.Add(node);
            _openScore.Add(score);
            var i = _open.Count - 1;
            while (i > 0)
            {
                var parent = (i - 1) / 2;
                if (_openScore[parent] <= _openScore[i])
                    break;

                SwapOpen(parent, i);
                i = parent;
            }
        }

        private int PopOpen()
        {
            var node = _open[0];
            var last = _open.Count - 1;
            _open[0] = _open[last];
            _openScore[0] = _openScore[last];
            _open.RemoveAt(last);
            _openScore.RemoveAt(last);

            var i = 0;
            while (true)
            {
                var left = i * 2 + 1;
                var right = left + 1;
                if (left >= _open.Count)
                    break;

                var smallest = right < _open.Count && _openScore[right] < _openScore[left] ? right : left;
                if (_openScore[i] <= _openScore[smallest])
                    break;

                SwapOpen(i, smallest);
                i = smallest;
            }

            return node;
        }

        private void SwapOpen(int a, int b)
        {
            (_open[a], _open[b]) = (_open[b], _open[a]);
            (_openScore[a], _openScore[b]) = (_openScore[b], _openScore[a]);
        }

        private void EnsureSearchBuffers(int count)
        {
            while (_cameFrom.Count < count)
            {
                _cameFrom.Add(-1);
                _gScore.Add(float.PositiveInfinity);
                _closed.Add(0);
            }
        }

        private static Vector3 WithHeight(Vector3 point, float heightOffset)
        {
            return new Vector3(point.x, point.y + heightOffset, point.z);
        }

        private static void Deduplicate(List<Vector3> path)
        {
            for (var i = path.Count - 1; i > 0; i--)
            {
                if ((path[i] - path[i - 1]).sqrMagnitude < 0.05f)
                    path.RemoveAt(i);
            }
        }
    }
}
