
using System.Runtime.InteropServices;

namespace InMemoryDataStoreManager
{


    public unsafe class SkipListUnsafe
    {
        struct Node
        {
            public int Value;
            public Node** Forward; // Ponteiro para array de ponteiros
            public int Level;      // Armazenar o nível para facilitar limpeza
        }

        const int MAX_LEVEL = 16;
        const double P = 0.5;
        private Node* head;
        private int level;
        private Random rand = new Random();

        public SkipListUnsafe()
        {
            // Alocar o nó head
            head = (Node*)NativeMemory.Alloc((nuint)sizeof(Node));
            head->Value = int.MinValue;
            head->Level = MAX_LEVEL;

            // Alocar array de ponteiros para o head
            head->Forward = (Node**)NativeMemory.Alloc((nuint)(sizeof(Node*) * (MAX_LEVEL + 1)));

            // Inicializar todos os ponteiros como null
            for (int i = 0; i <= MAX_LEVEL; i++)
            {
                head->Forward[i] = null;
            }

            level = 0;
        }

        private int RandomLevel()
        {
            int lvl = 0;
            while (rand.NextDouble() < P && lvl < MAX_LEVEL)
                lvl++;
            return lvl;
        }

        public void Insert(int value)
        {
            // Array temporário em memória gerenciada para updates
            Node*[] update = new Node*[MAX_LEVEL + 1];
            Node* x = head;

            for (int i = level; i >= 0; i--)
            {
                while (x->Forward[i] != null && x->Forward[i]->Value < value)
                    x = x->Forward[i];
                update[i] = x;
            }

            x = x->Forward[0];

            if (x == null || x->Value != value)
            {
                int lvl = RandomLevel();
                if (lvl > level)
                {
                    for (int i = level + 1; i <= lvl; i++)
                        update[i] = head;
                    level = lvl;
                }

                // Alocar novo nó
                Node* newNode = (Node*)NativeMemory.Alloc((nuint)sizeof(Node));
                newNode->Value = value;
                newNode->Level = lvl;

                // Alocar array de ponteiros para o novo nó
                newNode->Forward = (Node**)NativeMemory.Alloc((nuint)(sizeof(Node*) * (lvl + 1)));

                // Conectar o novo nó
                for (int i = 0; i <= lvl; i++)
                {
                    newNode->Forward[i] = update[i]->Forward[i];
                    update[i]->Forward[i] = newNode;
                }
            }
        }

        public bool Search(int value)
        {
            Node* x = head;
            for (int i = level; i >= 0; i--)
            {
                while (x->Forward[i] != null && x->Forward[i]->Value < value)
                    x = x->Forward[i];
            }
            x = x->Forward[0];
            return x != null && x->Value == value;
        }

        public void Delete(int value)
        {
            Node*[] update = new Node*[MAX_LEVEL + 1];
            Node* x = head;

            for (int i = level; i >= 0; i--)
            {
                while (x->Forward[i] != null && x->Forward[i]->Value < value)
                    x = x->Forward[i];
                update[i] = x;
            }

            x = x->Forward[0];

            if (x != null && x->Value == value)
            {
                for (int i = 0; i <= level; i++)
                {
                    if (update[i]->Forward[i] != x) break;
                    update[i]->Forward[i] = x->Forward[i];
                }

                while (level > 0 && head->Forward[level] == null)
                    level--;

                // Liberar a memória do array Forward e do nó
                NativeMemory.Free(x->Forward);
                NativeMemory.Free(x);
            }
        }

        // Implementar IDisposable para limpeza adequada
        public void Dispose()
        {
            // Liberar todos os nós
            Node* current = head->Forward[0];
            while (current != null)
            {
                Node* next = current->Forward[0];
                NativeMemory.Free(current->Forward);
                NativeMemory.Free(current);
                current = next;
            }

            // Liberar o head
            NativeMemory.Free(head->Forward);
            NativeMemory.Free(head);
        }
    }




    public class DataIndexer<T> where T : IComparable<T>
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
            var update = new IndexerNode<T>[MAX_LEVEL + 1];
            var x = head;

            // percorre cada nível do topo para baixo
            for (int i = level; i >= 0; i--)
            {
                while (x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];

            // se não existe, insere
            if (x == null || !x.Value.Equals(value))
            {
                int lvl = RandomLevel();
                if (lvl > level)
                {
                    for (int i = level + 1; i <= lvl; i++)
                        update[i] = head;
                    level = lvl;
                }

                x = new IndexerNode<T>(lvl, value);

                for (int i = 0; i <= lvl; i++)
                {
                    x.Forward[i] = update[i].Forward[i];
                    update[i].Forward[i] = x;
                }
            }
        }

        public void Delete(T value)
        {
            var update = new IndexerNode<T>[MAX_LEVEL + 1];
            var x = head;

            for (int i = level; i >= 0; i--)
            {
                while (x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
                update[i] = x;
            }

            x = x.Forward[0];

            if (x != null && x.Value.CompareTo(value) == 0)
            {
                for (int i = 0; i <= level; i++)
                {
                    if (update[i].Forward[i] != x) break;
                    update[i].Forward[i] = x.Forward[i];
                }

                while (level > 0 && head.Forward[level] == null)
                    level--;
            }
        }

        public bool Search(T value)
        {
            var x = head;

            // percorre do nível mais alto até o nível 0
            for (int i = level; i >= 0; i--)
            {
                while (x.Forward[i] != null && x.Forward[i].Value.CompareTo(value) < 0)
                    x = x.Forward[i];
            }

            x = x.Forward[0];

            // confere se encontrou
            return x != null && x.Value.CompareTo(value) == 0;
        }


        const double P         = 0.5;
        const int    MAX_LEVEL = 16;

        private IndexerNode<T>[] lastInserted = new IndexerNode<T>[MAX_LEVEL + 1];
        private readonly IndexerNode<T> head = new IndexerNode<T>(MAX_LEVEL, default!);
        private int level = 0;
        private readonly Random rand = new Random();

    }



    internal class IndexerNode<T> where T : IComparable<T>
    {
        public T Value;
        public IndexerNode<T>[] Forward;

        public IndexerNode(int level, T value)
        {
            Forward = new IndexerNode<T>[level + 1];
            Value   = value;
        }
    }


}
