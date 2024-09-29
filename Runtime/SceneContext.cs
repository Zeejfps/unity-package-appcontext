using UnityEngine;

namespace AppContextModule
{
    public abstract class SceneContext<T> : MonoBehaviour
    {
        private readonly Context _context = new();
        
        private void Awake()
        {
            var rootContext = App.RootContext;
            rootContext.AddChildContext(_context);
            Setup(_context);
        }

        private void OnDestroy()
        {
            Cleanup(_context);
            App.RootContext.RemoveChildContext(_context);
        }

        protected abstract void Setup(Context context);
        protected abstract void Cleanup(Context context);
    }
}