using Leopotam.EcsLite;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class NetworkSyncSystem : IEcsRunSystem
    {
        EcsWorld world;
        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var healthPool = world.GetPool<Health>();
            var viewPool = world.GetPool<ViewRef>();

            var filter = world.Filter<Health>().Inc<ViewRef>().End();
            foreach (var entity in filter)
            {
                ref var health = ref healthPool.Get(entity);
                ref var view = ref viewPool.Get(entity);
                if (view.View.Health != health.Current)
                    view.View.ServerSetHealth(health.Current);
            }
        }
    }
}
