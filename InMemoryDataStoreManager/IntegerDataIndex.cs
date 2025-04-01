
namespace InMemoryDataStoreManager
{

    public class IntegerDataIndex<Tdata>
    {

        public void Insert(int key, Tdata instance)
        {
            var clu = CurrentCluster(key, Cluster);

            if (key > clu.Max)
            {
                clu.Instances[clu.InstancesLength] = new IntIndexPoint<Tdata>() { Key = key };
                clu.InstancesLength++;
            }
            else
            {
                int left = 0;
                int found = binary_search(clu.Instances, ref left, clu.InstancesLength - 1, key);
                if (found < 0)
                {
                    for (int i = clu.InstancesLength; i > left; i--)
                    {
                        clu.Instances[i] = clu.Instances[i - 1];
                    }
                    clu.Instances[left] = new IntIndexPoint<Tdata>() { Key = key };
                    clu.InstancesLength++;
                }
                else
                {
                    throw new Exception("Duplicate key: " + key);
                }
            }
        }


        public Tdata Find(int key)
        {
            var clu = CurrentCluster(key, Cluster);
            if (key == clu.Max)
            {
                return clu.Instances[clu.InstancesLength - 1].Value;
            }
            else
            {
                int left = 0;
                int found = binary_search(clu.Instances, ref left, clu.InstancesLength - 1, key);
                if (found >= 0)
                {
                    return clu.Instances[found].Value;
                }
            }
            return default(Tdata);
        }


        public void Load()
        {

        }


        public static ClusterRange<Tdata> CurrentCluster(int key, ClusterRange<Tdata> cluster)
        {
            var clu = cluster.Children[cluster.ChildrenPosition];

            if (key > clu.Max)
            {
                if(!clu.IsLast)
                {
                    clu = CurrentCluster(key, clu);
                }
            }
            else
            {
                int ix = cluster.ChildrenPosition -1;
                while (ix > 1)
                {
                    if (key < cluster.Children[ix].First)
                    {
                        var cla = cluster.Children[ix-1];

                        if (key > cla.First)
                        {
                            if (!clu.IsLast)
                            {
                                return CurrentCluster(key, cla);
                            }
                            else
                            {
                                return cla;
                            } 
                        }
                        break;
                    }
                    ix++;
                }
            }
            return clu;
        }


        int binary_search(IntIndexPoint<Tdata>[] arr, ref int left, int right, int target)
        {
            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (arr[mid].Key == target)
                    return mid;
                if (arr[mid].Key < target)
                    left = mid + 1;
                else
                    right = mid - 1;
            }
            return -1;
        }

        private void CreateCluster(ClusterRange<Tdata> cluster, int size, int depth)
        {
            cluster.Children = new ClusterRange<Tdata>[size];
            var last = (size+1) > depth;

            int ix = 0;
            while (ix < size)
            {
                cluster.Children[ix] = new ClusterRange<Tdata>(ClusterSize) {IsLast = last };

                if (depth < MaxDepth)
                {
                    CreateCluster(cluster.Children[ix], size, depth + 1);
                }
                else
                {
                    cluster.Children[ix].Instances    = new IntIndexPoint<Tdata>[ClusterSize];
                    cluster.Children[ix].Instances[0] = new IntIndexPoint<Tdata>();
                }

                ix++;
            }
        }


        private ClusterRange<Tdata> Cluster;
        private readonly int ClusterSize;
        private readonly int MaxDepth;
        

        public IntegerDataIndex(int max_depth = 2, int cluster_size = 5000)
        {
            ClusterSize = cluster_size;
            MaxDepth    = max_depth;
            Cluster = new ClusterRange<Tdata>(ClusterSize);

            CreateCluster(Cluster, ClusterSize, MaxDepth);
        }


        static IntegerDataIndex()
        {

        }

    }

    public class ClusterRange<Tdata>
    {
        public int First;
        public int Max;
        public bool Completed;
        public bool IsLast;
        public int ChildrenPosition;
        public int InstancesPosition;
        public int InstancesLength;
        public ClusterRange<Tdata>[] Children;
        public IntIndexPoint<Tdata>[] Instances;

        public int Limit;

        public ClusterRange(int limit)
        {
            Limit = limit;

            Children  = new ClusterRange<Tdata>[limit];
            Instances = new IntIndexPoint<Tdata>[limit];
        }

    }

    public class IntIndexPoint<Tdata>
    {
        public int Key;
        public Tdata Value;

        public IntIndexPoint()
        {
            Key = -1;
        }
    }



}
