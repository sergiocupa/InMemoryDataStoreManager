using InMemoryDataStoreManager;
using System.Diagnostics;

namespace Tester
{

    public struct MyObject
    {
        public int ID;
        public string Name;
    }


    internal class Program
    {

        internal static double Integrate(double current, double previous, double Q)
        {
            double _int = (current + (previous * Q)) / (Q + 1.0);
            return _int;
        }

        static void Main(string[] args)
        {
			try
			{
                int MAX = 100000000;
                Console.WriteLine("Inserindo              " + MAX);

                var sw = Stopwatch.StartNew();
                sw.Restart();

                var list = new List<int>();
                int ix = 0;
                while (ix < MAX)
                {
                    ix++;
                    list.Add(ix);
                }

                sw.Stop();
                var t2 = sw.Elapsed.TotalMilliseconds;


                var clu = new ClusteredDictionary<string>(5000);

      
                const string A = "Name_";

                ix = 0;
                while(ix < 10)
                {
                    ix++;

                    //var obj = new MyObject()
                    //{
                    //    ID = ix,
                    //    Name = "Name_" + ix
                    //};
                    clu._Insert(ix, A);
                }

               
                sw.Restart();

              

                while (ix < MAX)
                {
                    ix++;
                    //var obj = new MyObject() { ID = ix, Name = "Name_" + ix };
                    clu._Insert(ix, A);
                }

                sw.Stop();
                var t1 = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine("Total cluster time     " + t1);
                

                int key = 90000000;
                Console.WriteLine("Buscar chave           " + key);

                sw.Restart();

                var res = list.Where(d => d == key).FirstOrDefault();


                sw.Stop();
                var t3 = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine("Total list time        " + t2);

                Console.WriteLine("Total pesquisa lista   " + t3);
                sw.Restart();


                var res2 = clu.Find(key);


                sw.Stop();
                var t4 = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine("Total pesquisa cluster " + t4);
                sw.Restart();

                Console.WriteLine("Dif                    " + (t3/t4));


            }
			catch (Exception ex)
			{
                Console.WriteLine(ex.ToString());
			}

            Thread.Sleep(new TimeSpan(1, 0, 0));
        }
    }
}
