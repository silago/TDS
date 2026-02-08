using UnityEngine;
using Zenject;
using TDS.Config;
using TDS.Net;

namespace TDS.Bootstrap
{
    public class ArenaInstaller : MonoInstaller
    {
        public GameConfig Config;

        public override void InstallBindings()
        {
            Container.Bind<GameConfig>().FromInstance(Config).AsSingle();
            Container.Bind<EntityRegistry>().AsSingle();
            Container.Bind<PickupRegistry>().AsSingle();
            Container.Bind<EcsContext>().AsSingle();
        }
    }
}
