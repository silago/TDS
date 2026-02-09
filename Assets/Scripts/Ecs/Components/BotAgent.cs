using UnityEngine;

namespace TDS.Ecs.Components
{
    public struct BotAgent
    {
        public float NextRepathTime;
        public Vector2 CurrentWaypoint;
        public bool HasWaypoint;
        public int TargetEntity;
    }
}
