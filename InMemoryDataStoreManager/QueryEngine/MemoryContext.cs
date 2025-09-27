
namespace InMemoryDataStoreManager.Engine
{

    public class MemoryContext
    {

        public static void Prepare<T>(Action<DataObjectInfo<T>> configure) where T : class
        {
            var t = typeof(T);
            if (Map.ContainsKey(t)) return;
            var artifact = new DataObjectInfo<T>();
            configure(artifact);
            Map[t] = artifact!;
        }

        public static DataObjectInfo<T> Get<T>() where T : class
        {
            var t = typeof(T);
            if (!Map.TryGetValue(t, out var obj))
                throw new InvalidOperationException($"Tipo {t.Name} não registrado em MemoryContext. Chame Prepare<{t.Name}> primeiro.");
            return (DataObjectInfo<T>)obj!;
        }



        private static readonly Dictionary<Type, object> Map;

        private MemoryContext() { }

        static MemoryContext()
        {
            Map = new();
        }

    }
}
