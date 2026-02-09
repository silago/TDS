using UnityEngine;

namespace TDS.Ecs.Components
{
    public struct InputData
    {
        public Vector2 Move;
        public Vector2 Aim;
        public bool Fire;
        public bool Melee;
        public bool Drop;
        public bool PrevDrop;
    }
}
