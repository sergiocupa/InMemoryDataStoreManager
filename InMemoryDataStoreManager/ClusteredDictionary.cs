using System.Diagnostics;


namespace InMemoryDataStoreManager
{

    public class ClusteredDictionary<Tdata>
    {

        public void _Insert(int key, Tdata instance)
        {
            var last = Ranges[Ranges.Count - 1];
            if (last.Instances.Count >= ClusterSize)
            {
                last = new ClusterRange<int, Tdata>() { First = key };
                Ranges.Add(last);
            }

            last.Instances.Add(new IndexPoint<int, Tdata>() { Key = key, Value = instance });
        }


        public void Insert(int key, Tdata instance)
        {
            //Console.WriteLine("Inserindo por chave " + key);

            var sw = Stopwatch.StartNew();

            var last = Ranges[Ranges.Count-1];
            if (last.Instances.Count >= ClusterSize)
            {
                //Console.WriteLine("Novo cluster");
                last = new ClusterRange<int, Tdata>() { First = key };
                Ranges.Add(last);
            }

            sw.Stop();
            var t1 = sw.ElapsedTicks;
            sw.Restart();

            if (Ranges.Count > 1)
            {
                ClusterRange<int, Tdata> target = null;

                //var sw = Stopwatch.StartNew();

                if (key >= last.First)
                {
                    target = last;
                }

                sw.Stop();
                var t2 = sw.ElapsedTicks;
                sw.Restart();

                if (target == null)
                {
                    int ix = Ranges.Count - 1;
                    while (ix > 1)
                    {
                        ix--;
                        if (key < Ranges[ix].First)// <
                        {
                            ix--;
                            target = Ranges[ix];
                            // Nao usa break, para parar ate identificar cluster alvo
                        }
                    }
                }

                sw.Stop();
                var t3 = sw.ElapsedTicks;
                sw.Restart();

                //sw.Stop();
                //var t1 = sw.Elapsed.TotalMilliseconds;
                //Console.WriteLine("Busca cluster " + t1);

                //if (key >= target.First)
                //{
                //    var im = target.Instances.Where(w => w.Value.Key == key).FirstOrDefault();
                //    if (im != null)
                //    {
                //        throw new Exception("Já existe");
                //    }
                //    //Tdata ir = default(Tdata);
                //    //if (target.Instances.TryGetValue(key, out ir))
                //    //{
                //    //    throw new Exception("Já existe");
                //    //}
                //}

                sw.Stop();
                var t4 = sw.ElapsedTicks;
                sw.Restart();

                target.Instances.Add(new IndexPoint<int, Tdata>() { Key = key, Value = instance });

                sw.Stop();
                var t5 = sw.ElapsedTicks;
                sw.Restart();
            }
            else
            {
                last.Instances.Add(new IndexPoint<int, Tdata>() { Key = key, Value = instance });
            }
        }

        public Tdata Find(int key)
        {
            var target = Ranges.LastOrDefault();

            if(key < target.First)
            {
                int ix = Ranges.Count - 1;
                while (ix > 1)
                {
                    ix--;
                    if (key < Ranges[ix].First && key >= Ranges[ix - 1].First)
                    {
                        ix--;
                        target = Ranges[ix];
                        break;
                    }
                }
            }

            int im = target.Instances.Count;
            while(im > 0)
            {
                im--;
                if(target.Instances[im].Value.Key == key)
                {
                    return target.Instances[im].Value.Value;
                }
            }
            return default(Tdata);
        }


        public void Load()
        {

        }


        private List<ClusterRange<int, Tdata>> Ranges;
        private readonly int ClusterSize;
        

        public ClusteredDictionary(int cluster_size)
        {
            ClusterSize = cluster_size;

            Ranges = new List<ClusterRange<int, Tdata>>();
            Ranges.Add(new ClusterRange<int, Tdata>());
        }



       // private static ComparerOp Comparer;

        static ClusteredDictionary()
        {
            //Comparer = new ComparerOp(typeof(int));
        }

    }

    public class ClusterRange<Tkey, Tdata>
    {
        public int First;
        public bool Completed;

        public List<IndexPoint<Tkey, Tdata>?> Instances;


        public ClusterRange()
        {
            Instances = new();
        }

    }

    public struct IndexPoint<Tkey, Tdata>
    {
        public Tkey Key;
        public Tdata Value;
    }



}
