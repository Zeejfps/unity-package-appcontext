namespace AppContextModule
{
    public sealed class App
    {
        public static T Get<T>()
        {
            var type = typeof(T);
            return (T)RootContext.Get(type);
        }

        public static Context RootContext { get; set; }
    }
}