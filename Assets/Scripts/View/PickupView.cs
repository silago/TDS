using Leopotam.EcsLite;
using Mirror;
using UnityEngine;
using TDS.Bootstrap;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.View
{
    [RequireComponent(typeof(Collider2D))]
    public class PickupView : NetworkBehaviour
    {
        public WeaponType WeaponType = WeaponType.Pistol;

        private int _entity = -1;

        public override void OnStartServer()
        {
            base.OnStartServer();

            var ctx = EcsBootstrap.Instance.Context;
            if (!ctx.ServerInitialized)
                ctx.InitializeServer();

            ctx.PickupRegistry.Register(this);

            var world = ctx.World;
            _entity = world.NewEntity();
            ref var pickup = ref world.GetPool<Pickup>().Add(_entity);
            pickup.WeaponType = WeaponType;
            ref var tr = ref world.GetPool<Transform2D>().Add(_entity);
            tr.Position = transform.position;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            var ctx = EcsBootstrap.Instance != null ? EcsBootstrap.Instance.Context : null;
            if (ctx != null)
            {
                ctx.PickupRegistry.Unregister(this);
                if (_entity >= 0 && ctx.World != null)
                    ctx.World.DelEntity(_entity);
            }
            _entity = -1;
        }

        [Server]
        public void ServerConsume()
        {
            var ctx = EcsBootstrap.Instance.Context;
            ctx.PickupRegistry.Unregister(this);
            NetworkServer.Destroy(gameObject);
        }
    }
}
