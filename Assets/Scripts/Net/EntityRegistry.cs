using System.Collections.Generic;
using TDS.View;

namespace TDS.Net
{
    public class EntityRegistry
    {
        private readonly Dictionary<uint, int> _netIdToEntity = new Dictionary<uint, int>();
        private readonly Dictionary<int, PlayerView> _entityToView = new Dictionary<int, PlayerView>();
        public readonly List<int> PlayerEntities = new List<int>();

        public void RegisterPlayer(uint netId, int entity, PlayerView view)
        {
            _netIdToEntity[netId] = entity;
            _entityToView[entity] = view;
            if (!PlayerEntities.Contains(entity))
                PlayerEntities.Add(entity);
        }

        public void UnregisterPlayer(uint netId)
        {
            if (_netIdToEntity.TryGetValue(netId, out int entity))
            {
                _netIdToEntity.Remove(netId);
                _entityToView.Remove(entity);
                PlayerEntities.Remove(entity);
            }
        }

        public bool TryGetEntity(uint netId, out int entity) => _netIdToEntity.TryGetValue(netId, out entity);

        public bool TryGetView(int entity, out PlayerView view) => _entityToView.TryGetValue(entity, out view);
    }
}
