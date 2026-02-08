using Mirror;
using UnityEngine;

namespace TDS.Net
{
    public class MatchStateView : NetworkBehaviour
    {
        [SyncVar] public float RemainingTime;
        public readonly SyncDictionary<uint, int> Scores = new SyncDictionary<uint, int>();
    }
}
