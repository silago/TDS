using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;
using TDS.View;

namespace TDS.Ecs.Systems
{
    public sealed class ShootingSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;
        private static int _nextBulletId = 1;
        private readonly Collider2D[] _meleeHits = new Collider2D[16];

        public ShootingSystem(GameConfig config, EntityRegistry registry)
        {
            _config = config;
            _registry = registry;
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var inputPool = world.GetPool<InputData>();
            var weaponPool = world.GetPool<Weapon>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var bulletPool = world.GetPool<Bullet>();
            var requestPool = world.GetPool<DamageRequest>();
            var deadPool = world.GetPool<Dead>();
            
            var filter =  world.Filter<InputData>().Inc<Weapon>().Inc<Transform2D>().Inc<ViewRef>().End();

            foreach (var entity in filter)
            {
                if (deadPool.Has(entity))
                    continue;

                ref var input = ref inputPool.Get(entity);
                if (!input.Fire)
                    continue;

                ref var weapon = ref weaponPool.Get(entity);
                if (weapon.Type == WeaponType.None)
                    continue;

                float now = Time.time;
                if (now < weapon.NextFireTime)
                    continue;

                if (weapon.FireMode != WeaponFireMode.Melee && weapon.Ammo <= 0)
                    continue;

                weapon.NextFireTime = now + weapon.FireCooldown;
                if (weapon.FireMode != WeaponFireMode.Melee)
                    weapon.Ammo -= 1;

                ref var tr = ref transformPool.Get(entity);
                Vector2 origin = tr.Position;
                Vector2 dir = input.Aim;
                ref var view = ref viewPool.Get(entity);
                view.View.ServerSetWeapon(weapon.Type, weapon.Ammo, weapon.MagSize);

                if (weapon.FireMode == WeaponFireMode.Melee)
                {
                    float meleeRadius = Mathf.Max(0.1f, weapon.Range);
                    Vector2 center = origin + dir.normalized * meleeRadius;
                    int count = Physics2D.OverlapCircleNonAlloc(center, meleeRadius, _meleeHits, _config.HitMask);
                    if (count <= 0)
                        continue;

                    for (int i = 0; i < count; i++)
                    {
                        var hit = _meleeHits[i];
                        if (hit == null)
                            continue;

                        var targetView = hit.GetComponentInParent<PlayerView>();
                        if (targetView == null || targetView.netId == view.View.netId)
                            continue;
                        if (!_registry.TryGetEntity(targetView.netId, out int targetEntity))
                            continue;
                        if (deadPool.Has(targetEntity))
                            continue;

                        int requestEntity = world.NewEntity();
                        ref var request = ref requestPool.Add(requestEntity);
                        request.TargetEntity = targetEntity;
                        request.Amount = weapon.Damage;
                        request.SourceNetId = view.View.netId;
                        targetView.RpcHit(view.View.netId, weapon.Damage);
                        break;
                    }

                    continue;
                }

                Vector2 muzzle = origin + dir.normalized * weapon.ShootOffset;
                int pellets = Mathf.Max(1, weapon.Pellets);
                float spread = Mathf.Max(0f, weapon.SpreadDeg);

                for (int i = 0; i < pellets; i++)
                {
                    Vector2 shotDir = dir;
                    if (spread > 0f)
                    {
                        float angle = Random.Range(-spread, spread);
                        shotDir = (Quaternion.Euler(0f, 0f, angle) * shotDir).normalized;
                    }

                    int bulletId = _nextBulletId++;
                    view.View.RpcSpawnBullet(weapon.Type, bulletId, muzzle, shotDir, weapon.BulletSpeed, weapon.Range);

                    int bulletEntity = world.NewEntity();
                    ref var bullet = ref bulletPool.Add(bulletEntity);
                    bullet.Id = bulletId;
                    bullet.Position = muzzle;
                    bullet.Direction = shotDir;
                    bullet.Speed = weapon.BulletSpeed;
                    bullet.RemainingDistance = weapon.Range;
                    bullet.Damage = weapon.Damage;
                    bullet.ShooterNetId = view.View.netId;
                }
            }
        }
    }
}
