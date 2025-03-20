using InMemoryDataStoreManager.Util;


namespace InMemoryDataStoreManager
{

    public class ClusteredDictionary<Tkey,Tdata>
    {


        public void Insert(Tkey key, Tdata instance)
        {
            //Console.WriteLine("Inserindo por chave " + key);

            var last = Ranges.LastOrDefault();
            if (last.Instances.Count >= ClusterSize)
            {
                //Console.WriteLine("Novo cluster");
                Ranges.Add(new ClusterRange<Tkey, Tdata>() { First = key });
            }

            if (Ranges.Count > 1)
            {
                ClusterRange<Tkey, Tdata> target = null;

                //var sw = Stopwatch.StartNew();

                int ix = Ranges.Count;
                while (ix > 1)
                {
                    ix--;
                    if (Comparer.LessThan.Compare(key, Ranges[ix].First))
                    {
                        ix--;
                        target = Ranges[ix];
                        // Nao usa break, para parar ate identificar cluster alvo
                    }
                }

                //sw.Stop();
                //var t1 = sw.Elapsed.TotalMilliseconds;
                //Console.WriteLine("Busca cluster " + t1);

                if (target == null)
                {
                    target = Ranges.LastOrDefault();

                    // Testar com entrada de chaves aleatorias se ira funcionar inserir chave aleatoria ja existente
                    if (Comparer.GreaterThanOrEqual.Compare(key, target.First))
                    {
                        Tdata ir = default(Tdata);
                        if (target.Instances.TryGetValue(key, out ir))
                        {
                            throw new Exception("Já existe");
                        }
                    }

                    //Console.WriteLine("Ultimo cluster " + Ranges.Count);
                }

                target.Instances.Add(key, instance);
            }
            else
            {
                var first = Ranges.FirstOrDefault();
                first.Instances.Add(key, instance);
                //Console.WriteLine("Ultimo cluster " + Ranges.Count);
            }
        }

        public Tdata Find(Tkey key)
        {
            ClusterRange<Tkey, Tdata> target = null;

            int ix = Ranges.Count;
            while (ix > 1)
            {
                ix--;
                if (Comparer.LessThan.Compare(key, Ranges[ix].First))
                {
                    ix--;
                    target = Ranges[ix];
                }
            }

            if (target == null)
            {
                target = Ranges.LastOrDefault();
            }

            Tdata ir = default(Tdata);
            target.Instances.TryGetValue(key, out ir);
            return ir;
        }


        public void Load()
        {

        }


        private List<ClusterRange<Tkey, Tdata>> Ranges;
        private readonly int ClusterSize;
        

        public ClusteredDictionary(int cluster_size)
        {
            ClusterSize = cluster_size;

            Ranges = new List<ClusterRange<Tkey, Tdata>>();
            Ranges.Add(new ClusterRange<Tkey, Tdata>());
        }



        private static ComparerOp Comparer;

        static ClusteredDictionary()
        {
            Comparer = new ComparerOp(typeof(Tkey));
        }

    }

    public class ClusterRange<Tkey, Tdata>
    {
        public Tkey First;
        public bool Completed;

        public SortedDictionary<Tkey, Tdata> Instances;


        public ClusterRange()
        {
            Instances = new SortedDictionary<Tkey, Tdata>();
        }

    }



}
