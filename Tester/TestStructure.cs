using InMemoryDataStoreManager;
using System.Diagnostics;


namespace Tester
{

    internal class TestStructureRunner
    {

        internal static void Run()
        {
            int nInserts = 1000000; // para 1 milhão, aumentar n
            Console.WriteLine("Rodando testes com " + nInserts + " registros…\r\n");

            // var skipResult   = TestStructure.Run(new DataIndexer<int>(), nInserts);
            //var skipUnsafe   = TestStructure.Run(new SkipListUnsafe(), nInserts);
            var skipResult   = TestStructure.Run(new DataIndexer<int>(), nInserts);
            var skipResult2  = TestStructure.Run(new DataIndexer2<int>(), nInserts);
            var PlusTree = TestStructure.Run(new BPlusTree<int,int>(), nInserts);
            var simpleResult = TestStructure.Run(new SimpleList<int>(), nInserts);

            var results = new List<TestResult> { skipResult, skipResult2, PlusTree, simpleResult };

            var csv = "Structure\tInsert Linear\tInsert Random\tSearch Linear\tSearch Random\tDelete Linear\tDelete Random\r\n";
            foreach (var r in results)
            {
                csv += $"{r.Structure}\t{r.InsertLinear.ToString("0.000000")}\t{r.InsertRandom.ToString("0.000000")}\t{r.SearchLinear.ToString("0.000000")}\t{r.SearchRandom.ToString("0.000000")}\t{r.DeleteLinear.ToString("0.000000")}\t{r.DeleteRandom.ToString("0.000000")}\r\n";
            }

            Console.Write(csv);

            string path = Path.Combine(Directory.GetCurrentDirectory(), "results.csv");
            using (var sw = new StreamWriter(path))
            {
                sw.Write(csv);
            }

        }



    }


    internal class TestStructure
    {

        internal static TestResult Run(dynamic structure, int nInserts)
        {
            var rand = new Random();
            var sw = new Stopwatch();

            // Inserção linear
            sw.Start();
            for (int i = 0; i < nInserts; i++)

                structure.Insert(i);
            sw.Stop();

            double insertLinear = sw.Elapsed.TotalSeconds;

            // Inserção aleatória
            dynamic s2 = Activator.CreateInstance(structure.GetType());
            var randomValues = Enumerable.Range(0, nInserts * 2).OrderBy(x => rand.Next()).Take(nInserts).ToList();
            sw.Restart();
            foreach (var v in randomValues)
                s2.Insert(v);
            sw.Stop();
            double insertRandom = sw.Elapsed.TotalSeconds;

            // Busca linear (primeiros 1000)
            sw.Restart();
            for (int i = 0; i < 1000; i++)
                structure.Search(i);
            sw.Stop();
            double searchLinear = sw.Elapsed.TotalSeconds;

            // Busca aleatória
            var searchValues = Enumerable.Range(0, nInserts * 2).OrderBy(x => rand.Next()).Take(1000).ToList();
            sw.Restart();
            foreach (var v in searchValues)
                s2.Search(v);
            sw.Stop();
            double searchRandom = sw.Elapsed.TotalSeconds;

            // Exclusão linear
            sw.Restart();
            for (int i = 0; i < 1000; i++)
                structure.Delete(i);
            sw.Stop();
            double deleteLinear = sw.Elapsed.TotalSeconds;

            // Exclusão aleatória
            var deleteValues = randomValues.OrderBy(x => rand.Next()).Take(1000).ToList();
            sw.Restart();
            foreach (var v in deleteValues)
                s2.Delete(v);
            sw.Stop();
            double deleteRandom = sw.Elapsed.TotalSeconds;

            return new TestResult
            {
                Structure = structure.GetType().Name,
                InsertLinear = insertLinear,
                InsertRandom = insertRandom,
                SearchLinear = searchLinear,
                SearchRandom = searchRandom,
                DeleteLinear = deleteLinear,
                DeleteRandom = deleteRandom
            };
        }

    }


    internal class TestResult
    {
        public string Structure { get; set; }
        public double InsertLinear { get; set; }
        public double InsertRandom { get; set; }
        public double SearchLinear { get; set; }
        public double SearchRandom { get; set; }
        public double DeleteLinear { get; set; }
        public double DeleteRandom { get; set; }
    }


    public class SimpleList<T> where T : IComparable<T>
    {
        private readonly List<T> data = new List<T>();
        public void Insert(T value)
        {
            data.Add(value);
            // Mantém ordenado
            //data.Sort();
        }
        public bool Search(T value) => data.Contains(value);
        public void Delete(T value) => data.Remove(value);
    }
}
