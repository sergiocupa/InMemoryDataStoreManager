//  MIT License – Modified for Mandatory Attribution
//  
//  Copyright(c) 2025 Sergio Paludo
//
//  github.com/sergiocupa
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files, 
//  to use, copy, modify, merge, publish, distribute, and sublicense the software, including for commercial purposes, provided that:
//  
//     01. The original author’s credit is retained in all copies of the source code;
//     02. The original author’s credit is included in any code generated, derived, or distributed from this software, including templates, libraries, or code - generating scripts.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.



using InMemoryDataStoreManager.Engine;
using InMemoryDataStoreManager.Indexer;
using System.Diagnostics;


namespace Tester
{

    internal class TestStructureRunner
    {

        internal static void Run()
        {
            int nInserts = 1000000; // para 1 milhão, aumentar n
            Console.WriteLine("Rodando testes com " + nInserts + " registros…\r\n");


            MemoryDataSource.Prepare<Movimento>((a) =>
            {
                a.AddIndex(a => a.Numero,false);
                a.AddIndex(a => a.ID, false);
            });
            MemoryDataSource.Prepare<Transito>((a) =>
            {
                a.AddIndex(a => a.ID, true);
            });


            var movs = MemoryDataSource.Get<Movimento>();
            var tran = MemoryDataSource.Get<Transito>();
            var movq = movs.AsQueryable();
            var tra  = tran.AsQueryable();

            tran.Save(new Transito() { ID = 2, Codigo = "002" });
            tran.Save(new Transito() { ID = 3, Codigo = "003" });
            tran.Save(new Transito() { ID = 4, Codigo = "004" });
            tran.Save(new Transito() { ID = 5, Codigo = "005" });

            movs.Save(new Movimento() { ID = 1,  CodigoFilial = "100", Serie = "200", Numero = 1 });
            movs.Save(new Movimento() { ID = 2,  CodigoFilial = "100", Serie = "200", Numero = 2 });
            movs.Save(new Movimento() { ID = 3,  CodigoFilial = "100", Serie = "200", Numero = 3 });
            movs.Save(new Movimento() { ID = 4,  CodigoFilial = "100", Serie = "200", Numero = 4 });
            movs.Save(new Movimento() { ID = 5,  CodigoFilial = "100", Serie = "200", Numero = 5 });
            movs.Save(new Movimento() { ID = 6,  CodigoFilial = "100", Serie = "200", Numero = 6 });
            movs.Save(new Movimento() { ID = 7,  CodigoFilial = "123", Serie = "200", Numero = 7 });
            movs.Save(new Movimento() { ID = 8,  CodigoFilial = "100", Serie = "200", Numero = 8 });
            movs.Save(new Movimento() { ID = 9,  CodigoFilial = "101", Serie = "200", Numero = 8 });
            movs.Save(new Movimento() { ID = 10, CodigoFilial = "100", Serie = "200", Numero = 9 });
            movs.Save(new Movimento() { ID = 11, CodigoFilial = "100", Serie = "200", Numero = 10 });


            var ff = from g in movq
                     join a in tra on g.Numero equals a.ID
                     select new { a, g };

            var hhh = ff.ToList();


            // Nao usa o Index, se campo com index misturado com campo sem index.
            var bb = movq.Where(e => e.Numero >= 8 && e.ID == 8 && e.CodigoFilial == "100").OrderBy(o => o.Numero).Select(f => new { f.CodigoFilial, f.Nome, f.ID }).ToArray();



            var index = new SkipList<int, Movimento>();
            //index.Insert(2,new Movimento());
            //index.Insert(3, new Movimento());
            //index.Insert(6, new Movimento());
            //index.Insert(9, new Movimento());
            //index.Insert(10, new Movimento());
            //index.Insert(13, new Movimento());
            //index.Insert(20, new Movimento());

            //var ss = default(int?);


            ////var exist = index.SearchRange(3, null, includeMin: false);
            ////var cnt   = exist.ToList();

            //var skipResult2  = TestStructure.Run(new SkipList<int,Movimento>(), nInserts);
            //var PlusTree     = TestStructure.Run(new BPlusTree<int,int>(), nInserts);
            //var simpleResult = TestStructure.Run(new SimpleList<int>(), nInserts);

            //var results = new List<TestResult> { skipResult2, PlusTree, simpleResult };

            //var csv = "Structure\tInsert Linear\tInsert Random\tSearch Linear\tSearch Random\tDelete Linear\tDelete Random\r\n";
            //foreach (var r in results)
            //{
            //    csv += $"{r.Structure}\t{r.InsertLinear.ToString("0.000000")}\t{r.InsertRandom.ToString("0.000000")}\t{r.SearchLinear.ToString("0.000000")}\t{r.SearchRandom.ToString("0.000000")}\t{r.DeleteLinear.ToString("0.000000")}\t{r.DeleteRandom.ToString("0.000000")}\r\n";
            //}

            //Console.Write(csv);

            //string path = Path.Combine(Directory.GetCurrentDirectory(), "results.csv");
            //using (var sw = new StreamWriter(path))
            //{
            //    sw.Write(csv);
            //}

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


    public class Movimento
    {
        public string  CodigoFilial { get; set; }
        public string Serie { get; set; }
        public int ID { get; set; }
        public int Numero { get; set; }
        public string Nome { get; set; }
    }

    public class Transito
    {
        public string Codigo { get; set; }
        public int ID { get; set; }
        public string Nome { get; set; }
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
