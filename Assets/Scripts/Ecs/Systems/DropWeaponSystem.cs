using Leopotam.EcsLite;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.Ecs.Systems
{
    public sealed class DropWeaponSystem : IEcsRunSystem
    {
        private readonly GameConfig _config;
        private readonly ArenaNetworkManager _netManager;

        public DropWeaponSystem(GameConfig config, ArenaNetworkManager netManager)
        {
            _config = config;
            _netManager = netManager;
        }

        public void Run(EcsSystems systems)
        {
            if (_netManager == null)
                return;

            var world = systems.GetWorld();
            var inputPool = world.GetPool<InputData>();
            var weaponPool = world.GetPool<Weapon>();
            var transformPool = world.GetPool<Transform2D>();
            var viewPool = world.GetPool<ViewRef>();
            var deadPool = world.GetPool<Dead>();

            var filter = world.Filter<InputData>().Inc<Weapon>().Inc<Transform2D>().Inc<ViewRef>().End();
            foreach (var entity in filter)
            {
                if (deadPool.Has(entity))
                    continue;

                ref var input = ref inputPool.Get(entity);
                bool dropPressed = input.Drop && !input.PrevDrop;
                input.PrevDrop = input.Drop;

                if (!dropPressed)
                    continue;

                ref var weapon = ref weaponPool.Get(entity);
                if (weapon.Type == WeaponType.None)
                    continue;
                if (!weapon.CanDrop)
                    continue;

                ref var tr = ref transformPool.Get(entity);
                ref var view = ref viewPool.Get(entity);

                _netManager.SpawnDroppedPickup(weapon.Type, tr.Position, view.View.netId);
                
                if (_config.TryGetWeapon(_config.DefaultWeaponType, out var defaultWeapon))
                    weapon.Apply(defaultWeapon);
                else
                    weapon.Clear();

                view.View.ServerSetWeapon(weapon.Type, weapon.Ammo, weapon.MagSize);
            }
        }
    }
}
