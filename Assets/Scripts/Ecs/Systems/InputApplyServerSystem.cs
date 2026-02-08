using Leopotam.EcsLite;
using UnityEngine;
using TDS.Ecs.Components;

namespace TDS.Ecs.Systems
{
    public sealed class InputApplyServerSystem : IEcsRunSystem
    {
        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var inputPool = world.GetPool<InputData>();
            var aimPool = world.GetPool<AimData>();

            var filter = world.Filter<InputData>().Inc<PlayerTag>().End();
            foreach (var entity in filter)
            {
                ref var input = ref inputPool.Get(entity);
                input.Move = Vector2.ClampMagnitude(input.Move, 1f);
                if (input.Aim.sqrMagnitude < 0.0001f)
                {
                    input.Aim = Vector2.right;
                }
                else
                {
                    input.Aim = input.Aim.normalized;
                }

                ref var aim = ref aimPool.Get(entity);
                aim.Direction = input.Aim;
            }
        }
    }
}
