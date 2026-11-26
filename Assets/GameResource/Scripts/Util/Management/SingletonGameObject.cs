using System.Text.RegularExpressions;
using UnityEngine;

namespace Backend.Util.Management
{
    public class SingletonGameObject<T> : SingletonBehavior where T : MonoBehaviour
    {
        #region CONSTANT FIELD API

        private const string SUFFIX = " (Singleton)";

        private const string PATTERN = "/([A-Z])(?=[A-Z][a-z])|([a-z])(?=[A-Z])/g";
        private const string REPLACEMENT = "$& ";

        #endregion

        private static T _instance;

        protected sealed override void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();
        }

        protected static T Instance
        {
            get
            {
                if (IsQuitting)
                {
                    UnityEngine.Debug.LogWarning("<color=#F7E600><b>[WARNING][Singleton]</b> Instance of type {typeof(T)} already destroyed on application quit. Returning null.</color>");

                    return null;
                }

                if (_instance != null)
                {
                    return _instance;
                }

                // Search for all existing singleton instance.
                var instances = FindObjectsByType<T>(FindObjectsSortMode.None);

                var length = instances.Length;
                if (0 < length)
                {
                    if (length == 1)
                    {
                        return _instance = instances[0];
                    }

                    UnityEngine.Debug.LogWarning($"<color=#F7E600><b>[WARNING][Singleton]</b> There should never be more than one singleton instance of type {typeof(T)} in the scene, but <b>{length}</b> were found. The first instance found will be used, and all others will be destroyed.</color>");

                    for (var i = 1; i < length; i++)
                    {
                        Destroy(instances[i]);
                    }

                    return _instance = instances[0];
                }

                // If it hasn't been created yet, create an instance.
                var clone = new GameObject();
                _instance = clone.AddComponent<T>();

                // Set singleton instance name.
                var name = Regex.Replace(typeof(T).Name, PATTERN, REPLACEMENT);
                clone.name = name + SUFFIX;

                UnityEngine.Debug.Log("<color=#F8F8FF><b>[Singleton]</b> An instance is needed in the scene and no existing instances were found, so a new instance will be created.</color>");

                return _instance;
            }
        }
    }
}
