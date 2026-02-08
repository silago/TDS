using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace TDS.Net
{
    public class ArenaNetworkManager : NetworkManager
    {
        public static ArenaNetworkManager Instance { get; private set; }

        [Header("Match")]
        public float MatchDuration = 180f;
        public MatchStateView MatchState;

        [Header("Spawns")]
        public List<Transform> SpawnPoints = new List<Transform>();

        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            CacheSpawnPoints();
            EnsureMatchState();
            if (MatchState != null)
                MatchState.RemainingTime = MatchDuration;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            var startPos = GetSpawnPosition();
            var player = Instantiate(playerPrefab, startPos, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn.identity != null && MatchState != null)
                MatchState.Scores.Remove(conn.identity.netId);

            base.OnServerDisconnect(conn);
        }

        public override void Update()
        {
            base.Update();
            if (!NetworkServer.active)
                return;

            if (MatchState != null && MatchState.RemainingTime > 0f)
                MatchState.RemainingTime -= Time.deltaTime;
        }

        public void AddKill(uint killerNetId)
        {
            if (killerNetId == 0)
                return;

            if (MatchState == null)
                return;

            if (!MatchState.Scores.ContainsKey(killerNetId))
                MatchState.Scores[killerNetId] = 0;

            MatchState.Scores[killerNetId] = MatchState.Scores[killerNetId] + 1;
        }

        public Vector2 GetSpawnPosition()
        {
            if (SpawnPoints.Count == 0)
                return Vector2.zero;

            int idx = Random.Range(0, SpawnPoints.Count);
            return SpawnPoints[idx].position;
        }

        private void CacheSpawnPoints()
        {
            if (SpawnPoints.Count > 0)
                return;

            var points = GameObject.FindGameObjectsWithTag("SpawnPoint");
            for (int i = 0; i < points.Length; i++)
                SpawnPoints.Add(points[i].transform);
        }

        private void EnsureMatchState()
        {
            if (MatchState != null)
                return;

            MatchState = FindFirstObjectByType<MatchStateView>();
        }
    }
}
