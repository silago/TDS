using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;
using TDS.View;

namespace TDS.Ecs.Systems
{
    public sealed class BulletSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;

        public BulletSystem(GameConfig config, EntityRegistry registry)
        {
            _config = config;
            _registry = registry;
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var bulletPool = world.GetPool<Bullet>();
            var damagePool = world.GetPool<DamageEvent>();
            var deadPool = world.GetPool<Dead>();
            var healthPool = world.GetPool<Health>();

            var filter = world.Filter<Bullet>().End();
            foreach (var entity in filter)
            {
                ref var bullet = ref bulletPool.Get(entity);

                float step = bullet.Speed * Time.deltaTime;
                if (step <= 0f)
                {
                    bulletPool.Del(entity);
                    continue;
                }

                float travel = Mathf.Min(step, bullet.RemainingDistance);
                Vector2 origin = bullet.Position;
                Vector2 dir = bullet.Direction;

                var hit = Physics2D.Raycast(origin, dir, travel, _config.HitMask);
                if (hit.collider != null)
                {
                    var targetView = hit.collider.GetComponentInParent<PlayerView>();
                    if (targetView != null && targetView.netId != bullet.ShooterNetId)
                    {
                        if (_registry.TryGetEntity(targetView.netId, out int targetEntity))
                        {
                            if (!deadPool.Has(targetEntity))
                            {
                                var health = healthPool.Get(targetEntity);
                                if (health.Current > 0)
                                {
                                    ref var dmg = ref damagePool.Get(targetEntity);
                                    dmg.Amount += bullet.Damage;
                                    dmg.SourceNetId = bullet.ShooterNetId;
                                    targetView.RpcHit(bullet.ShooterNetId, bullet.Damage);
                                }
                            }
                        }
                    }

                    if (_registry.TryGetViewByNetId(bullet.ShooterNetId, out var shooterView))
                        shooterView.RpcBulletHit(bullet.Id);

                    bulletPool.Del(entity);
                    continue;
                }

                bullet.Position = origin + dir * travel;
                bullet.RemainingDistance -= travel;

                if (bullet.RemainingDistance <= 0f)
                {
                    bulletPool.Del(entity);
                }
            }
        }
    }
}
