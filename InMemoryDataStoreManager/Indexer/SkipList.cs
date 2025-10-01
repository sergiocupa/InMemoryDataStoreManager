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
            var node = FindNode(key);
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

        public IEnumerable Find(object value)
        {
            return Find((Tkey)value);
        }

        public List<Tobj> Find(Tkey value)
        {
            var x = head;
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Key.CompareTo(value) < 0)
                    x = x.Forward[i];
            }
            x = x.Forward[0];
            return (x != null && x.Key.CompareTo(value) == 0) ? x.Objects : null;
        }

        private IndexerNode<Tkey, Tobj> FindNode(Tkey value)
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


        public List<JoinResult<Tkey, Tobj, TobjOther>> _SearchJoin<TobjOther>(SkipList<Tkey, TobjOther> other) where TobjOther : class
        {
            var results = new List<JoinResult<Tkey, Tobj, TobjOther>>();

            var leftNode  = this.head.Forward[0];
            var rightNode = other.head.Forward[0];

            while (leftNode != null && rightNode != null)
            {
                int cmp = leftNode.Key.CompareTo(rightNode.Key);

                if (cmp == 0)
                {
                    results.Add(new JoinResult<Tkey, Tobj, TobjOther>(leftNode.Key, new List<Tobj>(leftNode.Objects), new List<TobjOther>(rightNode.Objects)));

                    leftNode  = leftNode.Forward[0];
                    rightNode = rightNode.Forward[0];
                }
                else if (cmp < 0)
                {
                    leftNode = leftNode.Forward[0];
                }
                else
                {
                    rightNode = rightNode.Forward[0];
                }
            }

            return results;
        }

        /// <summary>
        /// Faz um JOIN entre este índice e outro índice usando a chave (Tkey).
        /// Retorna diretamente os pares (a,b) que possuem a mesma chave.
        /// </summary>
        public List<(Tobj Left, TOther Right)> SearchJoin<TOther>(SkipList<Tkey, TOther> other) where TOther : class
        {
            var results = new List<(Tobj, TOther)>();

            var leftNode = this.head.Forward[0];
            var rightNode = other.head.Forward[0];

            while (leftNode != null && rightNode != null)
            {
                int cmp = leftNode.Key.CompareTo(rightNode.Key);

                if (cmp == 0)
                {
                    // faz o produto cartesiano dos objetos dessa chave
                    foreach (var l in leftNode.Objects)
                    {
                        foreach (var r in rightNode.Objects)
                        {
                            results.Add((l, r));
                        }
                    }

                    leftNode  = leftNode.Forward[0];
                    rightNode = rightNode.Forward[0];
                }
                else if (cmp < 0)
                {
                    leftNode = leftNode.Forward[0];
                }
                else
                {
                    rightNode = rightNode.Forward[0];
                }
            }

            return results;
        }


        private int RandomLevel()
        {
            int lvl = 0;
            while (rand.NextDouble() < P && lvl < MAX_LEVEL)
                lvl++;
            return lvl;
        }

        public PropertyInfo GetProperty()
        {
            return Property;
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

    public class JoinResult<Tkey, TobjLeft, TobjRight> where Tkey : struct, IComparable<Tkey>
    {
        public Tkey Key { get; }
        public List<TobjLeft> Left { get; }
        public List<TobjRight> Right { get; }

        public JoinResult(Tkey key, List<TobjLeft> left, List<TobjRight> right)
        {
            Key = key;
            Left = left;
            Right = right;
        }
    }

}
