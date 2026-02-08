using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;
using TDS.View;

namespace TDS.Ecs.Systems
{
    public sealed class MeleeSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;
        private readonly Collider2D[] _hits = new Collider2D[16];
        private readonly ContactFilter2D contactFilter;

        public MeleeSystem(GameConfig config, EntityRegistry registry)
        {
            _config = config;
            _registry = registry;
            
            contactFilter = new ContactFilter2D
            {
                layerMask = _config.HitMask
            };
        }

        public void Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var inputPool = world.GetPool<InputData>();
            var meleePool = world.GetPool<Melee>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var damagePool = world.GetPool<DamageEvent>();
            var deadPool = world.GetPool<Dead>();

            var filter = world.Filter<InputData>().Inc<Melee>().Inc<Transform2D>().Inc<ViewRef>().End();
            foreach (var entity in filter)
            {
                if (deadPool.Has(entity))
                    continue;

                ref var input = ref inputPool.Get(entity);
                if (!input.Melee)
                    continue;

                ref var melee = ref meleePool.Get(entity);
                float now = Time.time;
                if (now < melee.NextTime)
                    continue;

                melee.NextTime = now + melee.Cooldown;

                ref var tr = ref transformPool.Get(entity);
                Vector2 center = tr.Position + input.Aim.normalized * melee.Range;


                int count = Physics2D.OverlapCircle(center, melee.Range,  contactFilter, _hits);
                if (count <= 0)
                    continue;

                ref var view = ref viewPool.Get(entity);
                for (int i = 0; i < count; i++)
                {
                    var targetView = _hits[i].GetComponentInParent<PlayerView>();
                    if (targetView == null || targetView.netId == view.View.netId)
                        continue;

                    if (!_registry.TryGetEntity(targetView.netId, out int targetEntity))
                        continue;

                    if (deadPool.Has(targetEntity))
                        continue;

                    ref var dmg = ref damagePool.Get(targetEntity);
                    dmg.Amount += melee.Damage;
                    dmg.SourceNetId = view.View.netId;

                    targetView.RpcHit(view.View.netId, melee.Damage);
                    break;
                }
            }
        }
    }
}
