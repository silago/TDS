using UnityEngine;

namespace TDS.View
{
    public class BulletVisual : MonoBehaviour
    {
        [HideInInspector] public int PrefabId;

        private Vector2 _dir;
        private Vector2 _start;
        private float _remaining;
        private float _speed;

        public void Init(Vector2 origin, Vector2 dir, float speed, float range)
        {
            _dir = dir.normalized;
            _start = origin;
            transform.position = origin;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
            _speed = Mathf.Max(0.01f, speed);
            _remaining = Mathf.Max(0.1f, range);
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            if (step <= 0f)
            {
                BulletPool.Return(this);
                return;
            }

            float travel = Mathf.Min(step, _remaining);
            transform.position += (Vector3)(_dir * travel);
            _remaining -= travel;
            if (_remaining <= 0f)
                BulletPool.Return(this);
        }
    }
}
