
namespace InMemoryDataStoreManager.Engine
{

    public class MemoryDataSource
    {

        public static void Prepare<T>(Action<ObjecProvider<T>> configure) where T : class
        {
            var t = typeof(T);
            if (Map.ContainsKey(t)) return;
            var artifact = new ObjecProvider<T>();
            configure(artifact);
            Map[t] = artifact!;
        }

        public static ObjecProvider<T> Get<T>() where T : class
        {
            var t = typeof(T);
            if (!Map.TryGetValue(t, out var obj))
                throw new InvalidOperationException($"Tipo {t.Name} não registrado em MemoryContext. Chame Prepare<{t.Name}> primeiro.");
            return (ObjecProvider<T>)obj!;
        }



        private static readonly Dictionary<Type, object> Map;

        private MemoryDataSource() { }

        static MemoryDataSource()
        {
            Map = new();
        }

    }
}
