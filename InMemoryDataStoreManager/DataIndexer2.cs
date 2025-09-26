
namespace InMemoryDataStoreManager
{

    public class DataIndexer2<T> where T : IComparable<T>
    {
        private int RandomLevel()
        {
            int lvl = 0;
            while (rand.NextDouble() < P && lvl < MAX_LEVEL)
                lvl++;
            return lvl;
        }

        public void Insert(T value)
        {
            // Otimização simples: verificar se pode inserir no final (inserção sequencial)
            if (tail == null || value.CompareTo(tail.Value) > 0)
            {
                InsertAtEnd(value);
                return;
            }

            // Inserção normal para casos não-sequenciais
            InsertNormal(value);
        }

        private void InsertAtEnd(T value)
        {
            int lvl = RandomLevel();
            var newNode = new IndexerNode<T>(lvl, value);

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


        private void InsertNormal(T value)
        {
            var update = new IndexerNode<T>[MAX_LEVEL + 1];
            var x = head;

            // Busca padrão da skip list
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];

            // Se não existe, insere
            if (x == null || !x.Value.Equals(value))
            {
                int lvl = RandomLevel();
                if (lvl > level)
                {
                    for (int i = level + 1; i <= lvl; i++)
                        update[i] = head;
                    level = lvl;
                }

                var newNode = new IndexerNode<T>(lvl, value);
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
        }

        // Método específico para inserção em lote sequencial
        public void InsertRange(IEnumerable<T> values)
        {
            var sortedValues = values.OrderBy(x => x);
            foreach (var value in sortedValues)
            {
                if (tail == null || value.CompareTo(tail.Value) > 0)
                    InsertAtEnd(value);
                else
                    InsertNormal(value);
            }
        }

        public void Delete(T value)
        {
            var update = new IndexerNode<T>[MAX_LEVEL + 1];
            var x = head;

            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];
            if (x != null && x.Value.CompareTo(value) == 0)
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
                    tail = (update[0] == head) ? null : update[0];
                }

                while (level > 0 && head.Forward[level] == null)
                    level--;
            }
        }

        public bool Search(T value)
        {
            var x = head;
            for (int i = level; i >= 0; i--)
            {
                while (i < x.Forward.Length && x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
            }
            x = x.Forward[0];
            return x != null && x.Value.CompareTo(value) == 0;
        }

        const double P = 0.5;
        const int MAX_LEVEL = 16;

        private readonly IndexerNode<T> head = new IndexerNode<T>(MAX_LEVEL, default!);
        private IndexerNode<T>? tail = null;
        private int level = 0;
        private readonly Random rand = new Random();
        private IndexerNode<T>[]? tailLevels = new IndexerNode<T>[MAX_LEVEL + 1];
    }



}
