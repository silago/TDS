using Mirror;
using UnityEngine;

namespace TDS.View
{
    public class CameraFollow : MonoBehaviour
    {
        public Vector3 Offset = new Vector3(0f, 0f, -10f);
        public float SmoothTime = 0.1f;

        private Transform _target;
        private Vector3 _vel;

        private void LateUpdate()
        {
            if (_target == null)
                TryFindTarget();

            if (_target == null)
                return;

            Vector3 desired = _target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, SmoothTime);
        }

        private void TryFindTarget()
        {
            if (NetworkClient.localPlayer != null)
                _target = NetworkClient.localPlayer.transform;
        }
    }
}
