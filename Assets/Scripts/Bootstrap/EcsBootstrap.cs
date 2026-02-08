using Mirror;
using UnityEngine;
using Zenject;

namespace TDS.Bootstrap
{
    public class EcsBootstrap : MonoBehaviour
    {
        public static EcsBootstrap Instance { get; private set; }
        public EcsContext Context => _context;

        [Inject] private EcsContext _context;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!NetworkServer.active)
                return;

            if (!_context.ServerInitialized)
                _context.InitializeServer();

            _context.RunServer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _context?.Dispose();
        }
    }
}
