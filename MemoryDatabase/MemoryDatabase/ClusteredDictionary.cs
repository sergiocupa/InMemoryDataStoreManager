using MemoryDatabase.Util;


namespace MemoryDatabase
{

    public class ClusteredDictionary<Tkey,Tdata>
    {


        public void Insert(Tkey key, Tdata instance)
        {
            Tdata ir = default(Tdata);
            var exist = Ranges.Where(w => w.Value.Instances.TryGetValue(key, out ir)).FirstOrDefault();
            if(exist.Value != null)
            {
                throw new Exception("Já existe");
            }

            var last = Ranges.LastOrDefault();
            if ((last.Value.Instances.Count + 1) >= ClusterSize)
            {
                Ranges.Add(key, new ClusterRange<Tkey, Tdata>());
            }

            if (Ranges.Count > 1)
            {
                ClusterRange<Tkey, Tdata> target = null;
                int ix = Ranges.Count;
                while(ix > 1)
                {
                    ix--;
                    if(Comparer.LessThan.Compare(key, Ranges.Keys[ix]))
                    {
                        ix--;
                        target = Ranges.Values[ix];
                    }
                }
                if (target == null) target = Ranges.Values[0];

                target.Instances.Add(key, instance);
            }
            else
            {
                var first = Ranges.FirstOrDefault();
                first.Value.Instances.Add(key, instance);
            }
        }

        public void Find<Tin>(Func<IQueryable<Tin>,IQueryable<Tin>> query)
        {

        }


        public void Load()
        {

        }


        private SortedList<Tkey, ClusterRange<Tkey, Tdata>> Ranges;

        private readonly int ClusterSize;
        

        public ClusteredDictionary(int cluster_size)
        {
            ClusterSize = cluster_size;

            Ranges = new SortedList<Tkey, ClusterRange<Tkey, Tdata>>();
            Ranges.Add(default(Tkey), new ClusterRange<Tkey, Tdata>());
        }



        private static ComparerOp Comparer;

        static ClusteredDictionary()
        {
            Comparer = new ComparerOp(typeof(Tkey));
        }

    }

    public class ClusterRange<Tkey, Tdata>
    {
        public ulong First;
        public ulong Last;
        public bool Completed;

        public SortedDictionary<Tkey, Tdata> Instances;


        public ClusterRange()
        {
            Instances = new SortedDictionary<Tkey, Tdata>();
        }

    }



}
