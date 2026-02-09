using Leopotam.EcsLite;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.Ecs.Systems
{
    public sealed class DropWeaponSystem : IEcsRunSystem
    {
        private readonly ArenaNetworkManager _netManager;

        public DropWeaponSystem(ArenaNetworkManager netManager)
        {
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

                ref var tr = ref transformPool.Get(entity);
                ref var view = ref viewPool.Get(entity);

                _netManager.SpawnDroppedPickup(weapon.Type, tr.Position, view.View.netId);

                weapon.Type = WeaponType.None;
                weapon.Range = 0f;
                weapon.Damage = 0;
                weapon.FireCooldown = 0f;
                weapon.NextFireTime = 0f;
                weapon.Ammo = 0;
                weapon.MagSize = 0;
                weapon.Pellets = 1;
                weapon.SpreadDeg = 0f;
                weapon.BulletSpeed = 0f;
                weapon.ShootOffset = 0f;

                view.View.ServerSetWeapon(WeaponType.None, 0, 0);
            }
        }
    }
}
