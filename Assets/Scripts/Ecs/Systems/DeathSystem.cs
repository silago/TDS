using Leopotam.EcsLite;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.Ecs.Systems
{
    public sealed class DeathSystem : IEcsRunSystem
    {
        EcsWorld world;
        private readonly GameConfig _config;
        private readonly ArenaNetworkManager _netManager;

        public DeathSystem(GameConfig config, ArenaNetworkManager netManager)
        {
            _config = config;
            _netManager = netManager;
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var healthPool = world.GetPool<Health>();
            var deadPool = world.GetPool<Dead>();
            var respawnPool = world.GetPool<Respawn>();
            var viewPool = world.GetPool<ViewRef>();

            var filter = world.Filter<Health>().Inc<ViewRef>().End();
            foreach (var entity in filter)
            {
                ref var health = ref healthPool.Get(entity);
                if (health.Current > 0 || deadPool.Has(entity))
                    continue;

                deadPool.Add(entity);
                ref var respawn = ref respawnPool.Add(entity);
                respawn.TimeLeft = _config.RespawnDelay;

                ref var view = ref viewPool.Get(entity);
                view.View.ServerSetAlive(false);
                view.View.RpcDied(health.LastDamager);

                if (_netManager != null && health.LastDamager != 0)
                    _netManager.AddKill(health.LastDamager);
            }
        }
    }
}
