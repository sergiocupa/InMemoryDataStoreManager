using System.Collections;
using System.Reflection;

namespace InMemoryDataStoreManager.Indexer
{

    public class SkipList<Tkey,Tobj> : IIndexer<Tkey, Tobj>, IIndexer where Tkey : struct, IComparable<Tkey>
    {

        public void Insert(object instance)
        {
            var vp = Property.GetValue(instance);
            Insert((Tkey)vp,(Tobj)instance);
        }

        public void Insert(Tkey key, Tobj instance)
        {
            var node = Find(key);
            if(node != null)
            {
                if (IsUnique)
                {
                    throw new InvalidOperationException($"Chave duplicada: {key}");
                }
                node.Objects.Add(instance);
                return;
            }

            // Otimização simples: verificar se pode inserir no final (inserção sequencial)
            if (tail == null || key.CompareTo(tail.Key) > 0)
            {
                InsertAtEnd(key, instance);
                return;
            }

            // Inserção normal para casos não-sequenciais
            InsertNormal(key, instance);
        }

        private void InsertAtEnd(Tkey key, Tobj instance)
        {
            //if(tail != null && tail.Key.CompareTo(key) == 0)
            //{
            //    if (IsUnique)
            //    {
            //        throw new InvalidOperationException($"Chave duplicada no final: {key}");
            //    }
            //}

            int lvl = RandomLevel();
            var newNode = new IndexerNode<Tkey, Tobj>(lvl, key, instance);

            if (tail == null)
            {
                // Primeiro elemento
                for (int i = 0; i <= lvl; i++)
                {
                    head.Forward[i] = newNode;
                    tailLevels[i] = newNode; // registrar novo tail por nível
                }
            }
            else
            {
                // Conectar ao final usando tail por nível
                for (int i = 0; i <= lvl; i++)
                {
                    if (tailLevels[i] != null)
                    {
                        tailLevels[i]!.Forward[i] = newNode;
                    }
                    else
                    {
                        // nível maior que o tail atual: conectar ao head
                        head.Forward[i] = newNode;
                    }
                    tailLevels[i] = newNode; // atualizar tail do nível
                }
            }

            // Atualizar nível global se necessário
            if (lvl > level)
            {
                for (int i = level + 1; i <= lvl; i++)
                {
                    head.Forward[i] = newNode;
                    tailLevels[i] = newNode;
                }
                level = lvl;
            }

            tail = newNode;
        }

        private void InsertNormal(Tkey key, Tobj instance)
        {
            var update = new IndexerNode<Tkey, Tobj>[MAX_LEVEL + 1];
            var x = head;

            // Busca padrão da skip list
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Key.CompareTo(key) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];

            // Se não existe, insere
            if (x == null || !x.Key.Equals(key))
            {
                int lvl = RandomLevel();
                if (lvl > level)
                {
                    for (int i = level + 1; i <= lvl; i++)
                        update[i] = head;
                    level = lvl;
                }

                var newNode = new IndexerNode<Tkey, Tobj>(lvl, key, instance);
                for (int i = 0; i <= lvl; i++)
                {
                    if (i < update[i].Forward.Length)
                    {
                        newNode.Forward[i] = update[i].Forward[i];
                        update[i].Forward[i] = newNode;
                    }
                }

                // Atualizar tail se inseriu no final
                if (newNode.Forward[0] == null)
                    tail = newNode;
            }
            //else
            //{
            //    if (IsUnique)
            //    {
            //        throw new InvalidOperationException($"Chave duplicada no final: {key}");
            //    } 
            //}
        }

        //// Método específico para inserção em lote sequencial
        //public void InsertRange(IEnumerable<Tuple<Tkey,Tobj>> values)
        //{
        //    var sortedValues = values.OrderBy(x => x);
        //    foreach (var value in sortedValues)
        //    {
        //        if (tail == null || value.Key.CompareTo(tail.Key) > 0)
        //            InsertAtEnd(value);
        //        else
        //            InsertNormal(value);
        //    }
        //}


        public void Delete(object value)
        {
            var vp = Property.GetValue(value);
            Delete((Tkey)vp);
        }
       
        public void Delete(Tkey value)
        {
            var update = new IndexerNode<Tkey,Tobj>[MAX_LEVEL + 1];
            var x = head;

            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Key.CompareTo(value) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];
            if (x != null && x.Key.CompareTo(value) == 0)
            {
                for (int i = 0; i <= level; i++)
                {
                    if (i < update[i].Forward.Length && update[i].Forward[i] == x)
                    {
                        if (i < x.Forward.Length)
                            update[i].Forward[i] = x.Forward[i];
                        else
                            update[i].Forward[i] = null;
                    }
                }

                // Atualizar tail se deletou o último elemento
                if (x == tail)
                {
                    tail = update[0] == head ? null : update[0];
                }

                while (level > 0 && head.Forward[level] == null)
                    level--;
            }
        }


        public bool Search(object value)
        {
            var vp = Property.GetValue(value);
            return Search((Tkey)vp);
        }

        public bool Search(Tkey value)
        {
            var x = head;
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Key.CompareTo(value) < 0)
                    x = x.Forward[i];
            }
            x = x.Forward[0];
            return x != null && x.Key.CompareTo(value) == 0;
        }

        public IndexerNode<Tkey,Tobj> Find(Tkey value)
        {
            var x = head;
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Key.CompareTo(value) < 0)
                    x = x.Forward[i];
            }
            x = x.Forward[0];
            return (x != null && x.Key.CompareTo(value) == 0) ? x : null;
        }


        public IEnumerable SearchRange(object minValue, object maxValue, bool includeMin = true, bool includeMax = true)
        {
            return SearchRange((Tkey?)minValue, (Tkey?)maxValue, includeMin, includeMax);
        }

        public IEnumerable<Tobj> SearchRange(Tkey minValue, Tkey maxValue, bool includeMin = true, bool includeMax = true)
        {
            return SearchRange(minValue, maxValue, includeMin, includeMax);
        }

        public IEnumerable<Tobj> SearchRange(Tkey? minValue, Tkey? maxValue, bool includeMin = true, bool includeMax = true) 
        {
            var result = new List<Tobj>();

            // Começa pelo head
            var x = head;

            // 1. Posicionar no ponto inicial da faixa
            for (int i = level; i >= 0; i--)
            {
                // se minValue for null, pega desde o início
                while (i < x.Forward.Length && x.Forward[i] != null && (minValue != null && x.Forward[i].Key.CompareTo(minValue.Value) < 0))
                {
                    x = x.Forward[i];
                }
            }

            // 2. Primeiro candidato
            x = x.Forward[0];

            // 3. Iterar enquanto não ultrapassar o maxValue
            while (x != null)
            {
                bool afterMin  = minValue == null || (includeMin ? x.Key.CompareTo(minValue.Value) >= 0 : x.Key.CompareTo(minValue.Value) > 0);
                bool beforeMax = maxValue == null || (includeMax ? x.Key.CompareTo(maxValue.Value) <= 0 : x.Key.CompareTo(maxValue.Value) < 0);

                if (afterMin && beforeMax)
                {
                    result.AddRange(x.Objects);
                }

                if (maxValue != null && (includeMax ? x.Key.CompareTo(maxValue.Value) > 0 : x.Key.CompareTo(maxValue.Value) >= 0)) break;

                x = x.Forward[0];
            }
            return result;
        }


        private int RandomLevel()
        {
            int lvl = 0;
            while (rand.NextDouble() < P && lvl < MAX_LEVEL)
                lvl++;
            return lvl;
        }

        const double P = 0.5;
        const int MAX_LEVEL = 16;

        private PropertyInfo Property;
        private readonly IndexerNode<Tkey, Tobj> head = new IndexerNode<Tkey, Tobj>(MAX_LEVEL, default!, default!);
        private IndexerNode<Tkey, Tobj>? tail = null;
        private int level = 0;
        private readonly Random rand = new Random();
        private IndexerNode<Tkey, Tobj>[]? tailLevels = new IndexerNode<Tkey, Tobj>[MAX_LEVEL + 1];
        private bool IsUnique;


        public SkipList()
        {
        }
        public SkipList(PropertyInfo property, bool is_unique)
        {
            Property = property;
            IsUnique = is_unique;
        }
    }


    public class IndexerNode<Tkey, Tobj> where Tkey : IComparable<Tkey>
    {
        public   Tkey                      Key;
        public   List<Tobj>                Objects;
        internal IndexerNode<Tkey, Tobj>[] Forward;

        internal IndexerNode(int level, Tkey key, Tobj instance)
        {
            Forward = new IndexerNode<Tkey, Tobj>[level + 1];
            Key = key;
            Objects = new List<Tobj>();
            Objects.Add(instance);
        }
    }

}
