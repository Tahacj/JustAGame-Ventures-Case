using System;
using System.Collections.Generic;
using UnityEngine;

namespace JustAGame.Pooling
{
    // Generic object pool to avoid Instantiate/Destroy GC allocations
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public int TotalCount => _pool.Count;

        public ObjectPool(
            T prefab,
            int initialCapacity = 10,
            Transform parent = null,
            Action<T> onGet = null,
            Action<T> onReturn = null)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new Queue<T>(initialCapacity);
            _onGet = onGet;
            _onReturn = onReturn;

            Prewarm(initialCapacity);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = CreateNewInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        private T CreateNewInstance()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        public T Get(Vector3 position = default, Quaternion rotation = default)
        {
            T instance;
            while (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
                if (instance != null)
                {
                    Transform t = instance.transform;
                    t.SetPositionAndRotation(position, rotation);
                    instance.gameObject.SetActive(true);
                    _onGet?.Invoke(instance);
                    return instance;
                }
            }

            // Pool exhausted; allocate new instance
            instance = CreateNewInstance();
            Transform newTransform = instance.transform;
            newTransform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            _onGet?.Invoke(instance);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null) return;

            _onReturn?.Invoke(instance);
            instance.gameObject.SetActive(false);
            if (_parent != null)
            {
                instance.transform.SetParent(_parent);
            }
            _pool.Enqueue(instance);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T instance = _pool.Dequeue();
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }
        }
    }

    // Component-based GameObject pool for scene inspector setup
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialPoolSize = 10;

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        private void Awake()
        {
            if (prefab != null)
            {
                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject obj = Instantiate(prefab, transform);
                    obj.SetActive(false);
                    _pool.Enqueue(obj);
                }
            }
        }

        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            GameObject obj;
            while (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
                if (obj != null)
                {
                    obj.transform.SetPositionAndRotation(position, rotation);
                    obj.SetActive(true);
                    return obj;
                }
            }

            obj = Instantiate(prefab, position, rotation, transform);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            _pool.Enqueue(obj);
        }

        private void OnDestroy()
        {
            while (_pool.Count > 0)
            {
                GameObject obj = _pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
    }
}
