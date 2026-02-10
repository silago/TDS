using Leopotam.EcsLite;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class DamageSystem : IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var requestPool = world.GetPool<DamageRequest>();
            var healthPool = world.GetPool<Health>();
            var deadPool = world.GetPool<Dead>();

            var filter = world.Filter<DamageRequest>().End();
            foreach (var requestEntity in filter)
            {
                ref var request = ref requestPool.Get(requestEntity);
                int targetEntity = request.TargetEntity;
                if (!healthPool.Has(targetEntity) || deadPool.Has(targetEntity))
                {
                    requestPool.Del(requestEntity);
                    continue;
                }

                ref var health = ref healthPool.Get(targetEntity);
                if (request.Amount > 0)
                    health.LastDamager = request.SourceNetId;

                health.Current -= request.Amount;
                if (health.Current < 0)
                    health.Current = 0;

                requestPool.Del(requestEntity);
            }
        }
    }
}
