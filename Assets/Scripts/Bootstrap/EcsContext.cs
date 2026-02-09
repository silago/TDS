using System;
using Leopotam.EcsLite;
using TDS.Config;
using TDS.Ecs.Systems;
using TDS.Net;

namespace TDS.Bootstrap
{
    public class EcsContext : IDisposable
    {
        private readonly GameConfig _config;
        private readonly EntityRegistry _registry;
        private readonly PickupRegistry _pickupRegistry;

        public GameConfig Config => _config;
        public EntityRegistry Registry => _registry;
        public PickupRegistry PickupRegistry => _pickupRegistry;

        public EcsWorld World { get; private set; }
        public EcsSystems ServerSystems { get; private set; }
        public bool ServerInitialized { get; private set; }

        public EcsContext(GameConfig config, EntityRegistry registry, PickupRegistry pickupRegistry)
        {
            _config = config;
            _registry = registry;
            _pickupRegistry = pickupRegistry;
        }

        public void InitializeServer()
        {
            if (ServerInitialized)
                return;

            World = new EcsWorld();
            ServerSystems = new EcsSystems(World);

            var netManager = ArenaNetworkManager.Instance;

            ServerSystems
                .Add(new BotInputSystem(_config, _registry))
                .Add(new InputApplyServerSystem())
                .Add(new MovementSystem())
                .Add(new AimSystem())
                .Add(new ShootingSystem(_config, _registry))
                .Add(new BulletSystem(_config, _registry))
                .Add(new DropWeaponSystem(netManager))
                .Add(new MeleeSystem(_config, _registry))
                .Add(new DamageSystem())
                .Add(new DeathSystem(_config, netManager))
                .Add(new RespawnSystem(_config, netManager))
                .Add(new PickupSystem(_config, _registry, _pickupRegistry))
                .Add(new NetworkSyncSystem());

            ServerSystems.Init();
            ServerInitialized = true;
        }

        public void RunServer()
        {
            ServerSystems?.Run();
        }

        public void Dispose()
        {
            if (ServerSystems != null)
            {
                ServerSystems.Destroy();
                ServerSystems = null;
            }

            if (World != null)
            {
                World.Destroy();
                World = null;
            }

            ServerInitialized = false;
        }
    }
}
