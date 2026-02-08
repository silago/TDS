using Leopotam.EcsLite;
using UnityEngine;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class MovementSystem : IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            
            
            var world = systems.GetWorld();
            var filter = world.Filter<InputData>().Inc<MoveSpeed>().Inc<Transform2D>().Inc<ViewRef>().End();
            var inputPool = world.GetPool<InputData>();
            var speedPool = world.GetPool<MoveSpeed>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var deadPool = world.GetPool<Dead>();

            foreach (var entity in filter)
            {
                if (deadPool.Has(entity))
                    continue;

                ref var input = ref inputPool.Get(entity);
                ref var speed = ref speedPool.Get(entity);
                ref var tr = ref transformPool.Get(entity);

                tr.Position += input.Move * speed.Value * Time.deltaTime;

                ref var view = ref viewPool.Get(entity);
                view.Transform.position = tr.Position;
            }
        }

    }
}
