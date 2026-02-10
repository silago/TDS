using System.Collections.Generic;
using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;
using TDS.View;

namespace TDS.Ecs.Systems
{
    public sealed class BotInputSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;
        private ArenaNavGrid _grid;
        private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[16];

        public BotInputSystem(GameConfig config, EntityRegistry registry)
        {
            _config = config;
            _registry = registry;
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var inputPool = world.GetPool<InputData>();
            var transformPool = world.GetPool<Transform2D>();
            var healthPool = world.GetPool<Health>();
            var weaponPool = world.GetPool<Weapon>();
            var botPool = world.GetPool<BotAgent>();
            var deadPool = world.GetPool<Dead>();

            if (_grid == null)
                _grid = ArenaNavGrid.Build(_config.HitMask, Mathf.Max(0.25f, _config.BotGridCellSize));

            var bots = world.Filter<BotTag>().Inc<PlayerTag>().Inc<InputData>().Inc<Transform2D>().Inc<Health>().Inc<Weapon>().End();
            foreach (var entity in bots)
            {
                if (deadPool.Has(entity))
                    continue;

                if (!botPool.Has(entity))
                    botPool.Add(entity);

                ref var bot = ref botPool.Get(entity);
                ref var input = ref inputPool.Get(entity);
                ref var tr = ref transformPool.Get(entity);
                ref var weapon = ref weaponPool.Get(entity);
                uint selfNetId = 0;
                if (_registry.TryGetView(entity, out var selfView) && selfView != null)
                    selfNetId = selfView.netId;

                if (!TryPickTarget(entity, world, deadPool, healthPool, transformPool, out int targetEntity, out Vector2 targetPos))
                {
                    input.Move = Vector2.zero;
                    input.Aim = Vector2.right;
                    input.Fire = false;
                    input.Drop = false;
                    continue;
                }

                bot.TargetEntity = targetEntity;

                Vector2 toTarget = targetPos - tr.Position;
                float distance = toTarget.magnitude;
                Vector2 aimDir = distance > 0.001f ? (toTarget / distance) : Vector2.right;
                input.Aim = aimDir;

                if (distance > _config.BotStopDistance && Time.time >= bot.NextRepathTime)
                {
                    bot.NextRepathTime = Time.time + Mathf.Max(0.1f, _config.BotRepathInterval);
                    bot.HasWaypoint = _grid != null && _grid.TryFindNextWaypoint(tr.Position, targetPos, out bot.CurrentWaypoint);
                }

                Vector2 move = Vector2.zero;
                if (distance > _config.BotStopDistance)
                {
                    if (bot.HasWaypoint)
                    {
                        Vector2 toWaypoint = bot.CurrentWaypoint - tr.Position;
                        if (toWaypoint.sqrMagnitude <= 0.05f * 0.05f)
                        {
                            bot.HasWaypoint = false;
                        }
                        else
                        {
                            move = toWaypoint.normalized;
                        }
                    }

                    if (!bot.HasWaypoint)
                        move = aimDir;
                }

                bool wantsFire;
                if (weapon.Type == WeaponType.None)
                {
                    wantsFire = false;
                }
                else if (weapon.FireMode == WeaponFireMode.Melee)
                {
                    float meleeRange = Mathf.Max(0.1f, weapon.Range) * 1.2f;
                    wantsFire = distance <= meleeRange;
                }
                else
                {
                    wantsFire = distance <= _config.BotFireDistance &&
                                distance <= Mathf.Max(0.1f, weapon.Range) &&
                                HasLineOfSight(tr.Position, targetPos, targetEntity, selfNetId);
                }

                input.Move = move;
                input.Fire = wantsFire;
                input.Drop = false;
            }
        }

        private bool TryPickTarget(
            int self,
            EcsWorld world,
            EcsPool<Dead> deadPool,
            EcsPool<Health> healthPool,
            EcsPool<Transform2D> transformPool,
            out int targetEntity,
            out Vector2 targetPos)
        {
            targetEntity = -1;
            targetPos = Vector2.zero;

            if (!transformPool.Has(self))
                return false;

            Vector2 selfPos = transformPool.Get(self).Position;
            float bestDistSqr = float.MaxValue;
            var players = _registry.PlayerEntities;
            for (int i = 0; i < players.Count; i++)
            {
                int candidate = players[i];
                if (candidate == self || deadPool.Has(candidate) || !healthPool.Has(candidate) || !transformPool.Has(candidate))
                    continue;

                ref var health = ref healthPool.Get(candidate);
                if (health.Current <= 0)
                    continue;

                Vector2 delta = transformPool.Get(candidate).Position - selfPos;
                float distSqr = delta.sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    targetEntity = candidate;
                    targetPos = transformPool.Get(candidate).Position;
                }
            }

            return targetEntity >= 0;
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to, int targetEntity, uint selfNetId)
        {
            if (!_registry.TryGetView(targetEntity, out var targetView) || targetView == null)
                return false;

            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist <= 0.001f)
                return true;

            Vector2 dir = delta / dist;
            Vector2 origin = from + dir * 0.2f;
            float castDist = Mathf.Max(0f, dist - 0.2f);
            int count = Physics2D.RaycastNonAlloc(origin, dir, _rayHits, castDist, _config.HitMask);
            if (count <= 0)
                return true;

            float nearestDistance = float.MaxValue;
            bool foundAnyRelevantHit = false;
            bool targetVisible = false;

            for (int i = 0; i < count; i++)
            {
                var hit = _rayHits[i];
                if (hit.collider == null)
                    continue;

                if (hit.distance >= nearestDistance)
                    continue;

                var hitView = hit.collider.GetComponentInParent<PlayerView>();
                if (hitView != null && hitView.netId == selfNetId)
                    continue;

                nearestDistance = hit.distance;
                foundAnyRelevantHit = true;
                targetVisible = hitView != null && hitView.netId == targetView.netId;
            }

            if (!foundAnyRelevantHit)
                return true;

            return targetVisible;
        }

        private sealed class ArenaNavGrid
        {
            private readonly float _cellSize;
            private readonly Vector2 _origin;
            private readonly int _width;
            private readonly int _height;
            private readonly bool[] _walkable;
            private readonly int[] _cameFrom;
            private readonly int[] _gScore;
            private readonly bool[] _closed;
            private readonly List<int> _open = new List<int>(256);

            private static readonly Collider2D[] OverlapHits = new Collider2D[16];

            private ArenaNavGrid(float cellSize, Vector2 origin, int width, int height, bool[] walkable)
            {
                _cellSize = cellSize;
                _origin = origin;
                _width = width;
                _height = height;
                _walkable = walkable;
                int size = width * height;
                _cameFrom = new int[size];
                _gScore = new int[size];
                _closed = new bool[size];
            }

            public static ArenaNavGrid Build(LayerMask obstacleMask, float cellSize)
            {
                var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
                Bounds bounds = default;
                bool hasBounds = false;

                int mask = obstacleMask.value;
                for (int i = 0; i < colliders.Length; i++)
                {
                    var c = colliders[i];
                    if (c == null || c.isTrigger)
                        continue;
                    if ((mask & (1 << c.gameObject.layer)) == 0)
                        continue;
                    if (c.GetComponentInParent<PlayerView>() != null)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = c.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(c.bounds);
                    }
                }

                if (!hasBounds)
                    return null;

                bounds.Expand(cellSize * 4f);

                int width = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x / cellSize), 8, 512);
                int height = Mathf.Clamp(Mathf.CeilToInt(bounds.size.y / cellSize), 8, 512);
                var walkable = new bool[width * height];

                Vector2 origin = new Vector2(bounds.min.x, bounds.min.y);
                Vector2 halfExtents = Vector2.one * (cellSize * 0.45f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 cellCenter = origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                        int count = Physics2D.OverlapBoxNonAlloc(cellCenter, halfExtents * 2f, 0f, OverlapHits, obstacleMask);
                        bool blocked = false;
                        for (int i = 0; i < count; i++)
                        {
                            var hit = OverlapHits[i];
                            if (hit == null)
                                continue;
                            if (hit.isTrigger)
                                continue;
                            if (hit.GetComponentInParent<PlayerView>() != null)
                                continue;
                            blocked = true;
                            break;
                        }

                        walkable[y * width + x] = !blocked;
                    }
                }

                return new ArenaNavGrid(cellSize, origin, width, height, walkable);
            }

            public bool TryFindNextWaypoint(Vector2 worldStart, Vector2 worldGoal, out Vector2 waypoint)
            {
                waypoint = worldGoal;
                if (!TryWorldToCell(worldStart, out int sx, out int sy))
                    return false;
                if (!TryWorldToCell(worldGoal, out int gx, out int gy))
                    return false;

                if (!TryFindNearestWalkable(sx, sy, out sx, out sy))
                    return false;
                if (!TryFindNearestWalkable(gx, gy, out gx, out gy))
                    return false;

                int start = ToIndex(sx, sy);
                int goal = ToIndex(gx, gy);
                if (start == goal)
                    return false;

                int size = _width * _height;
                for (int i = 0; i < size; i++)
                {
                    _cameFrom[i] = -1;
                    _gScore[i] = int.MaxValue;
                    _closed[i] = false;
                }

                _open.Clear();
                _gScore[start] = 0;
                _open.Add(start);

                int iterations = 0;
                const int maxIterations = 2048;
                while (_open.Count > 0 && iterations < maxIterations)
                {
                    iterations++;
                    int current = PopBestNode(goal);
                    if (current == goal)
                        break;

                    _closed[current] = true;
                    int cx = current % _width;
                    int cy = current / _width;

                    EvaluateNeighbor(cx + 1, cy, current, goal);
                    EvaluateNeighbor(cx - 1, cy, current, goal);
                    EvaluateNeighbor(cx, cy + 1, current, goal);
                    EvaluateNeighbor(cx, cy - 1, current, goal);
                }

                if (_cameFrom[goal] < 0)
                    return false;

                int step = goal;
                while (_cameFrom[step] >= 0 && _cameFrom[step] != start)
                    step = _cameFrom[step];

                int wx = step % _width;
                int wy = step / _width;
                waypoint = CellCenter(wx, wy);
                return true;
            }

            private void EvaluateNeighbor(int nx, int ny, int current, int goal)
            {
                if (!IsInBounds(nx, ny))
                    return;

                int neighbor = ToIndex(nx, ny);
                if (!_walkable[neighbor] || _closed[neighbor])
                    return;

                int tentative = _gScore[current] + 10;
                if (tentative >= _gScore[neighbor])
                    return;

                _cameFrom[neighbor] = current;
                _gScore[neighbor] = tentative;
                if (!_open.Contains(neighbor))
                    _open.Add(neighbor);
            }

            private int PopBestNode(int goal)
            {
                int bestIdx = 0;
                int bestNode = _open[0];
                int bestCost = FScore(bestNode, goal);
                for (int i = 1; i < _open.Count; i++)
                {
                    int node = _open[i];
                    int cost = FScore(node, goal);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestNode = node;
                        bestIdx = i;
                    }
                }

                _open.RemoveAt(bestIdx);
                return bestNode;
            }

            private int FScore(int node, int goal)
            {
                int x1 = node % _width;
                int y1 = node / _width;
                int x2 = goal % _width;
                int y2 = goal / _width;
                int h = Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
                return _gScore[node] + h * 10;
            }

            private bool TryFindNearestWalkable(int sx, int sy, out int rx, out int ry)
            {
                if (IsInBounds(sx, sy) && _walkable[ToIndex(sx, sy)])
                {
                    rx = sx;
                    ry = sy;
                    return true;
                }

                const int maxRadius = 8;
                for (int r = 1; r <= maxRadius; r++)
                {
                    for (int y = sy - r; y <= sy + r; y++)
                    {
                        for (int x = sx - r; x <= sx + r; x++)
                        {
                            if (!IsInBounds(x, y))
                                continue;
                            if (!_walkable[ToIndex(x, y)])
                                continue;
                            rx = x;
                            ry = y;
                            return true;
                        }
                    }
                }

                rx = sx;
                ry = sy;
                return false;
            }

            private bool TryWorldToCell(Vector2 world, out int x, out int y)
            {
                x = Mathf.FloorToInt((world.x - _origin.x) / _cellSize);
                y = Mathf.FloorToInt((world.y - _origin.y) / _cellSize);
                return IsInBounds(x, y);
            }

            private Vector2 CellCenter(int x, int y)
            {
                return _origin + new Vector2((x + 0.5f) * _cellSize, (y + 0.5f) * _cellSize);
            }

            private bool IsInBounds(int x, int y)
            {
                return x >= 0 && y >= 0 && x < _width && y < _height;
            }

            private int ToIndex(int x, int y)
            {
                return y * _width + x;
            }
        }
    }
}
