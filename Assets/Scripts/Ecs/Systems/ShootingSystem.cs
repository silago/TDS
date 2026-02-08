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
            var damagePool = world.GetPool<DamageEvent>();
            var deadPool = world.GetPool<Dead>();
            var healthPool = world.GetPool<Health>();
            
            var filter =  world.Filter<InputData>().Inc<Weapon>().Inc<Transform2D>().Inc<ViewRef>().End();

            foreach (var entity in filter)
            {

                ref var input = ref inputPool.Get(entity);
                if (!input.Fire)
                    continue;

                ref var weapon = ref weaponPool.Get(entity);
                if (weapon.Type == WeaponType.None)
                    continue;

                float now = Time.time;
                if (now < weapon.NextFireTime)
                    continue;

                if (weapon.Ammo <= 0)
                    continue;

                weapon.NextFireTime = now + weapon.FireCooldown;
                weapon.Ammo -= 1;

                ref var tr = ref transformPool.Get(entity);
                Vector2 origin = tr.Position;
                Vector2 dir = input.Aim;

                ref var view = ref viewPool.Get(entity);
                view.View.ServerSetWeapon(weapon.Type, weapon.Ammo, weapon.MagSize);

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

                    view.View.RpcSpawnBullet(weapon.Type, origin, shotDir, weapon.BulletSpeed, weapon.Range);

                    int bulletEntity = world.NewEntity();
                    ref var bullet = ref bulletPool.Add(bulletEntity);
                    bullet.Position = origin;
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
