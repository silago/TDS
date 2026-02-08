using UnityEngine;

namespace TDS.Ecs.Components
{
    public struct HitscanShotEvent
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public float Range;
        public uint ShooterNetId;
    }
}
