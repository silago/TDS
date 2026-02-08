using Leopotam.EcsLite;
using UnityEngine;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class AimSystem : IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var aimPool = world.GetPool<AimData>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var deadPool = world.GetPool<Dead>();

            var filter = world.Filter<AimData>().Inc<Transform2D>().Inc<ViewRef>().End();
            foreach (var entity in filter)
            {
                if (deadPool.Has(entity))
                    continue;

                ref var aim = ref aimPool.Get(entity);
                ref var tr = ref transformPool.Get(entity);
                float angle = Mathf.Atan2(aim.Direction.y, aim.Direction.x) * Mathf.Rad2Deg;
                tr.RotationDeg = angle;

                ref var view = ref viewPool.Get(entity);
                view.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
