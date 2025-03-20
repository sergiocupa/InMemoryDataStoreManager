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

        internal static double Integrate(double valorAtual, double valorAnterior, double Q, double samble_interval)
        {
            if (samble_interval == 0) samble_interval = 1;
            var sampling_rate = 1.0 / samble_interval;
            var tau = Q * sampling_rate;

            double _int = (valorAtual + (valorAnterior * tau)) / (tau + 1.0);
            return _int;
        }

        static void Main(string[] args)
        {
			try
			{
                double tt = 0;

                var clu = new ClusteredDictionary<int,MyObject>(50000);

                int MAX = 1000000;
                Console.WriteLine("Inserindo              " + MAX);

                int ix = 0;
                while(ix < 10)
                {
                    ix++;

                    var obj = new MyObject()
                    {
                        ID = ix,
                        Name = "Name_" + ix
                    };
                    clu.Insert(ix, obj);
                }

                var sw = Stopwatch.StartNew();

                while (ix < MAX)
                {
                    ix++;
                    var obj = new MyObject() { ID = ix, Name = "Name_" + ix };
                    clu.Insert(ix, obj);
                }

                sw.Stop();
                var t1 = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine("Total cluster time     " + t1);
                


                sw.Restart();

                var list = new List<MyObject>();
                ix = 0;
                while(ix < MAX)
                {
                    ix++;
                    list.Add(new MyObject() { ID = ix, Name = "Name_" + ix });
                }

                sw.Stop();
                var t2 = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine("Total list time        " + t2);
                
                int key = 900000;
                Console.WriteLine("Buscar chave           " + key);

                sw.Restart();

                var res = list.Where(d => d.ID == key).FirstOrDefault();


                sw.Stop();
                var t3 = sw.Elapsed.TotalMilliseconds;
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
