using UnityEngine;
using TDS.View;

namespace TDS.Ecs.Components
{
    public struct ViewRef
    {
        public Transform Transform;
        public PlayerView View;
        public Rigidbody2D Rb;
    }
}
