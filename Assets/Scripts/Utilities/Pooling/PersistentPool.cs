using System;
using System.Collections.Generic;
using UnityEngine;
using ReusableObject = UnityEngine.Object;
using HashKey = System.Int32;

namespace Core.Pooling
{
    public class PersistentPool
    {
        private class Pool
        {
            private readonly HashSet<HashKey> _instanceTracker = new();
            private readonly List<ReusableObject> _instanceCache = new();
            private readonly Stack<ReusableObject> _inactiveStack = new();
            public int NextID() => _instanceTracker.Count;
            public void TrackInstance(ReusableObject instance)
            {
                _instanceTracker.Add(instance.GetHashCode());
                _instanceCache.Add(instance);
            }
            public bool Borrow(out ReusableObject instance)
            {
                if (_inactiveStack.TryPop(out instance)) return true;
                return false;
            }
            public void Restore(ReusableObject instance)
            {
                instance.GetTransform()
                    .Parental(s_Instance._container)
                    .SetActive(false);
                _inactiveStack.Push(instance);
            }
            public void RestoreAll()
            {
                foreach (var instance in _instanceCache)
                {
                    if (instance.ActiveSelf())
                        Restore(instance);
                }
            }
            public bool Contains(ReusableObject instance) => _instanceTracker.Contains(instance.GetHashCode());
            public void Clear()
            {
                _instanceCache.ForEach(ReusableObject.Destroy);
                _instanceTracker.Clear();
                _instanceCache.Clear();
                _inactiveStack.Clear();
            }
        }
        
        static PersistentPool() => s_Instance = new PersistentPool();

        ~PersistentPool()
        {
            s_Instance._container = null;
            foreach (var kvp in s_Instance._pools)
                kvp.Value.Clear();
            s_Instance._pools.Clear();
            s_Instance = null;
        }

        private static PersistentPool s_Instance;
        
        /**
         * <key>HashCode of the prefab object.</key>
         * <value>The Pool Handler associated with that prefab.</value>
         */
        private readonly Dictionary<HashKey, Pool> _pools = new();

        private Transform _container;

        private PersistentPool()
        {
            _container = new GameObject("[PoolContainer]").transform;
            _container.gameObject.SetActive(false);
            _container.position = Vector3.zero;
            // _container.gameObject.hideFlags = HideFlags.HideInHierarchy;
            ReusableObject.DontDestroyOnLoad(_container.gameObject);
        }

        #region ➤ Internal Pool Management

        private Pool GetOrCreatePool(HashKey key)
        {
            if (_pools.TryGetValue(key, out var reusableObject))
                return reusableObject;
            reusableObject = new Pool();
            _pools.Add(key, reusableObject);
            return reusableObject;
        }

        private T Borrow<T>(T prefab) where T : ReusableObject
        {
            var pool = GetOrCreatePool(prefab.GetHashCode());
            if (pool.Borrow(out var result))
                return (T)result;
            result = ReusableObject.Instantiate(prefab);
            result.name = $"{prefab.name}_{pool.NextID()}";
            pool.TrackInstance(result);
            return (T)result;
        }
        
        private T BorrowComponent<T>(GameObject prefab) where T : Component
        {
            var pool = GetOrCreatePool(HashCode.Combine(prefab.GetHashCode(), typeof(T).GetHashCode()));
            if (pool.Borrow(out var result))
                return (T)result;
            result = ReusableObject.Instantiate(prefab).GetComponent<T>();
            if (result == null)
                throw new NullReferenceException($"Prefab {prefab.name} not contains {typeof(T).Name} component.");
            result.name = $"{prefab.name}_{pool.NextID()}";
            pool.TrackInstance(result);
            return (T)result;
        }

        private static void SetPosition(ReusableObject src, Vector3 pos) 
            => src.GetTransform().Parental(null)
                .SetPosition(pos).SetActive(true);
        
        private static void SetPositionAndRotation(ReusableObject src, Vector3 pos, Quaternion rot) 
            => src.GetTransform().Parental(null)
                .SetPosition(pos).SetRotation(rot)
                .SetActive(true);
        
        private static void SetPositionAndRotationParental(ReusableObject src, Vector3 pos, Quaternion rot, Transform parent, bool localSpace = false)
        {
            if (!localSpace)
                src.GetTransform().Parental(parent)
                    .SetPosition(pos).SetRotation(rot)
                    .SetActive(true);
            else
                src.GetTransform().Parental(parent)
                    .SetLocalPosition(pos).SetLocalRotation(rot)
                    .SetActive(true);
        }

        #endregion

        #region ➤ Public API

        public static void Clean()
        {
            foreach (var kvp in s_Instance._pools)
                kvp.Value.Clear();
            s_Instance._pools.Clear();
        }

        public static void Preload<T>(T prefab, int quantity) where T : ReusableObject
        {
            if(quantity <= 0) return;
            var pool = s_Instance.GetOrCreatePool(prefab.GetHashCode());
            for (var _ = 0; _ < quantity; _++)
                pool.Restore(s_Instance.Borrow(prefab));
        }
        
        public static void Preload<T>(GameObject prefab, int quantity) where T : Component
        {
            if(quantity <= 0) return;
            var pool = s_Instance.GetOrCreatePool(HashCode.Combine(prefab.GetHashCode(), typeof(T).GetHashCode()));
            for (var _ = 0; _ < quantity; _++)
                pool.Restore(s_Instance.BorrowComponent<T>(prefab));
        }
        
        public static T Get<T>(T prefab) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            result.GetTransform().Parental(null)
                .SetActive(true);
            return result;
        }
        
        public static T GetComponent<T>(GameObject prefab) where T : Component
        {
            var result = s_Instance.BorrowComponent<T>(prefab);
            result.GetTransform().Parental(null)
                .SetActive(true);
            return result;
        }

        public static T Get<T>(T prefab, Vector3 position) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPosition(result, position);
            return result;
        }
        
        public static T GetComponent<T>(GameObject prefab, Vector3 position) where T : Component
        {
            var result = s_Instance.BorrowComponent<T>(prefab);
            SetPosition(result, position);
            return result;
        }

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPositionAndRotation(result, position, rotation);
            return result;
        }
        
        public static T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var result = s_Instance.BorrowComponent<T>(prefab);
            SetPositionAndRotation(result, position, rotation);
            return result;
        }
        
        public static T Get<T>(T prefab, Vector3 position, Transform parent, bool localSpace = false) where T : ReusableObject
            => Get(prefab, position, prefab.GetTransform().rotation, parent, localSpace);
        
        public static T GetComponent<T>(GameObject prefab, Vector3 position, Transform parent, bool localSpace = false) where T : Component
            => GetComponent<T>(prefab, position, prefab.transform.rotation, parent, localSpace);

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, bool localSpace = false) where T : ReusableObject
        {
            var result = s_Instance.Borrow(prefab);
            SetPositionAndRotationParental(result, position, rotation, parent, localSpace);
            return result;
        }
        
        public static T GetComponent<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool localSpace = false) where T : Component
        {
            var result = s_Instance.BorrowComponent<T>(prefab);
            SetPositionAndRotationParental(result, position, rotation, parent, localSpace);
            return result;
        }

        /**
         * Releases an instance back to its pool, deactivating it.
         * If the instance does not belong to any known pool, it is destroyed. 
         */
        public static void Recycle<T>(T instance) where T : ReusableObject
        {
            foreach (var pool in s_Instance._pools.Values)
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

        public static void RecycleAll<T>(T prefab) where T : ReusableObject
        {
            if (s_Instance._pools.TryGetValue(prefab.GetHashCode(), out var pool))
                pool.RestoreAll();
        }

        public static void RecycleAll()
        {
            foreach (var kvp in s_Instance._pools)
                kvp.Value.RestoreAll();
        }

        #endregion
    }
}