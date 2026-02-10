using Leopotam.EcsLite;
using UnityEngine;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.Ecs.Systems
{
    public sealed class PickupSystem : IEcsRunSystem
    {
        EcsWorld world;
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;
        private readonly PickupRegistry _pickupRegistry;

        public PickupSystem(GameConfig config, EntityRegistry registry, PickupRegistry pickupRegistry)
        {
            _config = config;
            _registry = registry;
            _pickupRegistry = pickupRegistry;
        }

        public void Run(EcsSystems systems)
        {
            if (_pickupRegistry == null)
                return;

            var world = systems.GetWorld();
            var weaponPool = world.GetPool<Weapon>();
            var deadPool = world.GetPool<Dead>();

            var pickups = _pickupRegistry.ServerPickups;
            if (pickups.Count == 0)
                return;

            var players = _registry.PlayerEntities;
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var pickup = pickups[i];
                if (pickup == null)
                    continue;

                Vector2 pickupPos = pickup.transform.position;
                for (int p = 0; p < players.Count; p++)
                {
                    int playerEntity = players[p];
                    if (deadPool.Has(playerEntity))
                        continue;

                    if (!_registry.TryGetView(playerEntity, out var view))
                        continue;

                    float dist = Vector2.Distance(pickupPos, view.transform.position);
                    if (dist > _config.PickupRadius)
                        continue;

                    ref var weapon = ref weaponPool.Get(playerEntity);
                    if (weapon.Type == pickup.WeaponType)
                        continue;

                    if (pickup.IgnoredOwnerNetId != 0 &&
                        view.netId == pickup.IgnoredOwnerNetId &&
                        Time.time < pickup.OwnerIgnoreUntilTime)
                    {
                        continue;
                    }

                    if (_config.TryGetWeapon(pickup.WeaponType, out var cfg))
                    {
                        weapon.Apply(cfg);
                        view.ServerSetWeapon(weapon.Type, weapon.Ammo, weapon.MagSize);
                    }

                    pickup.ServerConsume();
                    break;
                }
            }
        }

    }
}
