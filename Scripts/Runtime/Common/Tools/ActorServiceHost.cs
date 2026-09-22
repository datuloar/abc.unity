using System;

using UnityEngine;

namespace Abc.Unity
{
    internal abstract class ActorServiceHost<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;

        protected static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                if (_isQuitting)
                    throw new InvalidOperationException($"Cannot create {typeof(T).Name} while the application is quitting.");

#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                _instance = FindObjectOfType<T>(true);
#endif
                if (_instance != null)
                    return _instance;

                var singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                singletonObject.hideFlags = Application.isPlaying
                    ? HideFlags.HideInHierarchy
                    : HideFlags.HideAndDontSave;
                _instance = singletonObject.AddComponent<T>();
                return _instance;
            }
        }

        protected static bool TryGetInstance(out T instance)
        {
            instance = _instance;
            return instance != null;
        }

        protected virtual void Awake()
        {
            var self = this as T;

            if (_instance != null && !ReferenceEquals(_instance, self))
            {
                enabled = false;

                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this);

                return;
            }

            _instance = self;

            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        protected static void ResetSingletonState()
        {
            _instance = null;
            _isQuitting = false;
        }

        protected virtual void OnApplicationQuit() => _isQuitting = true;

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
