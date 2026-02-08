using UnityEngine;

namespace TDS.Ecs.Components
{
    public struct Bullet
    {
        public Vector2 Position;
        public Vector2 Direction;
        public float Speed;
        public float RemainingDistance;
        public int Damage;
        public uint ShooterNetId;
    }
}
