using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using ReusableObject = UnityEngine.Object;

namespace Core.Pooling
{
    public class ObjectPool
    {
        private class Pool
        {
            private readonly HashSet<ReusableObject> _instanceTracker = new();
            private readonly Stack<ReusableObject> _inactiveStack = new();
            public int NextID() => _instanceTracker.Count;
            public void TrackInstance(ReusableObject instance) => _instanceTracker.Add(instance);
            public bool Contains(ReusableObject instance) => _instanceTracker.Contains(instance);
            public bool Borrow(out ReusableObject instance) => _inactiveStack.TryPop(out instance);
            public void Restore(ReusableObject instance)
            {
                if(_inactiveStack.Contains(instance))
                    return;
                _inactiveStack.Push(instance.SetActive(false));
            }

            public void RestoreAll()
            {
                foreach (var instance in _instanceTracker.Where(entry => entry.ActiveSelf()))
                    Restore(instance);
            }
            
            public void Clear()
            {
                _instanceTracker.Clear();
                _inactiveStack.Clear();
            }
        }

        static ObjectPool() => s_Instance = new ObjectPool();

        public ObjectPool()
        {
            SceneManager.sceneUnloaded += HandleOnSceneUnloaded;
        }

        private void HandleOnSceneUnloaded(Scene _) => Clean();

        ~ObjectPool()
        {
            s_Instance.m_Pools.Clear();
            s_Instance = null;
        }

        private static ObjectPool s_Instance;

        private readonly Dictionary<ReusableObject, Pool> m_Pools = new();

        #region 🔐 Internal Pool Management

        private Pool GetOrCreatePool(ReusableObject key)
        {
            if (m_Pools.TryGetValue(key, out var pool))
                return pool;
            pool = new Pool();
            m_Pools.Add(key, pool);
            return pool;
        }

        private T Borrow<T>(T prefab) where T : ReusableObject
        {
            var pool = GetOrCreatePool(prefab);
            if (pool.Borrow(out var result))
                return (T)result;
            result = ReusableObject.Instantiate(prefab);
            result.name = $"{prefab.name}_{pool.NextID()}";
            pool.TrackInstance(result);
            return (T)result;
        }

        private static void SetPosition(ReusableObject src, Vector3 pos)
            => src.GetTransform()
                .SetPosition(pos)
                .SetActive(true);

        private static void SetPositionAndRotation(ReusableObject src, Vector3 pos, Quaternion rot)
            => src.GetTransform()
                .SetPosition(pos)
                .SetRotation(rot)
                .SetActive(true);

        private static void SetPositionAndRotationParental(ReusableObject src, Vector3 pos, Quaternion rot, Transform parent, Space relativeTo = Space.World)
        {
            switch (relativeTo)
            {
                case Space.World:
                    src.GetTransform().Parental(parent)
                        .SetPosition(pos).SetRotation(rot)
                        .SetActive(true);
                    break;
                case Space.Self:
                    src.GetTransform().Parental(parent)
                        .SetLocalPosition(pos).SetLocalRotation(rot)
                        .SetActive(true);
                    break;
            }
        }

        #endregion

        #region 🌏 Public API

        public static void Preload<T>(T prefab, int quantity) where T : ReusableObject
        {
            if (quantity <= 0) return;
            var pool = s_Instance.GetOrCreatePool(prefab);
            for (var _ = 0; _ < quantity; _++)
                pool.Restore(s_Instance.Borrow(prefab));
        }

        public static T Get<T>(T prefab) where T : ReusableObject => s_Instance.Borrow(prefab).SetActive(true);

        public static T Get<T>(T prefab, Vector3 position) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPosition(result, position);
            return result;
        }

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPositionAndRotation(result, position, rotation);
            return result;
        }

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, Space relativeTo = Space.World) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPositionAndRotationParental(result, position, rotation, parent, relativeTo);
            return result;
        }

        public static T Get<T>(T prefab, Vector3 position, Transform parent, Space relativeTo = Space.World) where T : ReusableObject
            => Get(prefab, position, prefab.GetTransform().rotation, parent, relativeTo);

        public static T Get<T>(T prefab, Transform parent, Space relativeTo = Space.World) where T : ReusableObject
        {
            var prefabTransform = prefab.GetTransform();
            return Get(prefab, prefabTransform.position, prefabTransform.rotation, parent, relativeTo);
        }

        /**
         * Release an instance back to its pool, deactivating it.
         * If the instance does not belong to any pool, it is destroyed.
         */
        public static void Recycle<T>(T instance) where T : ReusableObject
        {
            foreach (var pool in s_Instance.m_Pools.Values)
            {
                if (!pool.Contains(instance))
                    continue;
                pool.Restore(instance);
                return;
            }
#if UNITY_EDITOR
            Debug.LogWarning($"<color=yellow><b><i>{instance.name} is not object pooling, it will be destroyed.</i></b></color>");
#endif
            ReusableObject.Destroy(instance);
        }
        
        /** Release all instance has been activated back to them pool and deactivating them. */
        public static void RecycleAll()
        {
            foreach (var pool in s_Instance.m_Pools.Values)
                pool.RestoreAll();
        }
        
        public static void Clean()
        {
            foreach (var kvp in s_Instance.m_Pools)
                kvp.Value.Clear();
            s_Instance.m_Pools.Clear();
        }

        #endregion
    }
}