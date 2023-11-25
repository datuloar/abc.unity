using UnityEngine;

namespace abc.unity.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static readonly object _lockObj = new object();
        private static T _instance;

        public static T Instance
        {
            get
            {
                lock (_lockObj)
                {
                    if (_instance != null)
                        return _instance;

                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance != null)
                        return _instance;

                    _instance = new GameObject($"{typeof(T).Name} (Singleton)").AddComponent<T>();
                    return _instance;
                }
            }

            private set
            {
                lock (_lockObj)
                {
                    _instance = value;
                }
            }
        }

        private void Awake()
        {
            if (_instance == null)
                Instance = GetComponent<T>();
        }

        public static void DeleteInstance()
        {
            Instance = null;
        }

        protected void OnDestroy()
        {
            if (_instance == this)
                DeleteInstance();
        }
    }
}