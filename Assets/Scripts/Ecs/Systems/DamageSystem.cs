using Leopotam.EcsLite;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class DamageSystem : IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var dmgPool = world.GetPool<DamageEvent>();
            var healthPool = world.GetPool<Health>();

            var filter = world.Filter<DamageEvent>().Inc<Health>().End();
            foreach (var entity in filter)
            {
                ref var dmg = ref dmgPool.Get(entity);
                ref var health = ref healthPool.Get(entity);
                if (dmg.Amount > 0)
                    health.LastDamager = dmg.SourceNetId;
                health.Current -= dmg.Amount;
                if (health.Current < 0)
                    health.Current = 0;

                dmg.Amount = 0;
                dmgPool.Del(entity);
            }
        }
    }
}
