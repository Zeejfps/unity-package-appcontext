using UnityEngine;

namespace AppContextModule
{
    public abstract class GlobalContext<T> : MonoBehaviour
    {
        protected static GlobalContext<T> _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Setup(App.RootContext);
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            Cleanup(App.RootContext);
            _instance = null;
        }

        protected abstract void Setup(Context context);
        protected abstract void Cleanup(Context context);
    }
}