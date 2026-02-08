using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.Ecs.Systems
{
    public sealed class RespawnSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly ArenaNetworkManager _netManager;

        public RespawnSystem(GameConfig config, ArenaNetworkManager netManager)
        {
            _config = config;
            _netManager = netManager;
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var respawnPool = world.GetPool<Respawn>();
            var deadPool = world.GetPool<Dead>();
            var healthPool = world.GetPool<Health>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var weaponPool = world.GetPool<Weapon>();

            var filter = world.Filter<Respawn>().Inc<Transform2D>().Inc<ViewRef>().Inc<Health>().End();
            foreach (var entity in filter)
            {
                ref var respawn = ref respawnPool.Get(entity);
                respawn.TimeLeft -= Time.deltaTime;
                if (respawn.TimeLeft > 0f)
                    continue;

                respawnPool.Del(entity);
                if (deadPool.Has(entity))
                    deadPool.Del(entity);

                ref var health = ref healthPool.Get(entity);
                health.Current = health.Max;
                health.LastDamager = 0;

                if (weaponPool.Has(entity))
                {
                    ref var weapon = ref weaponPool.Get(entity);
                    if (weapon.MagSize > 0)
                        weapon.Ammo = weapon.MagSize;
                }

                Vector2 spawn = _netManager != null ? _netManager.GetSpawnPosition() : Vector2.zero;
                ref var tr = ref transformPool.Get(entity);
                tr.Position = spawn;

                ref var view = ref viewPool.Get(entity);
                view.Transform.position = spawn;
                view.View.ServerSetHealth(health.Current);
                view.View.ServerSetAlive(true);
            }
        }
    }
}
