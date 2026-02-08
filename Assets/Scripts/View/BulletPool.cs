using System.Collections.Generic;
using UnityEngine;

namespace TDS.View
{
    public static class BulletPool
    {
        private static readonly Dictionary<int, Stack<BulletVisual>> Pools = new Dictionary<int, Stack<BulletVisual>>();

        public static BulletVisual Get(BulletVisual prefab)
        {
            if (prefab == null)
                return null;

            int id = prefab.GetInstanceID();
            if (!Pools.TryGetValue(id, out var stack))
            {
                stack = new Stack<BulletVisual>(16);
                Pools[id] = stack;
            }

            if (stack.Count > 0)
            {
                var inst = stack.Pop();
                if (inst != null)
                {
                    inst.gameObject.SetActive(true);
                    inst.PrefabId = id;
                    return inst;
                }
            }

            var created = Object.Instantiate(prefab);
            created.PrefabId = id;
            return created;
        }

        public static void Return(BulletVisual instance)
        {
            if (instance == null)
                return;

            int id = instance.PrefabId;
            if (id == 0)
            {
                Object.Destroy(instance.gameObject);
                return;
            }

            if (!Pools.TryGetValue(id, out var stack))
            {
                stack = new Stack<BulletVisual>(16);
                Pools[id] = stack;
            }

            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }
}
