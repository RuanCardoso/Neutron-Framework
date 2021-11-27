using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Wrappers;
using System;

namespace NeutronNetwork
{
    //* N�o use ConcurrentQueue, por algum motivo essa porra tem vazamento de mem�ria no met�do Enqueue(), aloca demais, e o GC Congela a unity quando inicia a limpeza.
    public class NeutronPool<T>
    {
        //* Inicializa um queue para o pool.
        private readonly NeutronSafeQueueNonAlloc<T> _queue;
        //* Objeto que ir� gerar novas inst�ncias quando necess�rio.
        private readonly Func<T> _generator;

        /// <summary>
        ///* Define se a capacidade � aumentada conforme o necess�rio.
        /// </summary>
        public bool CreateNewObjectIfNotAvailable
        {
            get;
        }

        /// <summary>
        ///* Nome do pool de objetos.
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        ///* Quantidade de objetos no pool.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 
        /// </summary>
        public NeutronSafeQueueNonAlloc<T> Queue => _queue;

        /// <summary>
        /// Inicializa um novo pool do tipo especificado em T.
        /// </summary>
        public NeutronPool(Func<T> generator, int capacity, bool createNewObjectIfNotAvailable, string name)
        {
            _generator = generator;
            _queue = new NeutronSafeQueueNonAlloc<T>(capacity);
            CreateNewObjectIfNotAvailable = createNewObjectIfNotAvailable;
            Name = name;
        }

        /// <summary>
        ///* Obt�m um objeto dispon�vel do pool de objetos.
        /// </summary>
        [ThreadSafe]
        public T Pull()
        {
            if (_queue.TryPull(out T item))
                return item;
            else
            {
                if (CreateNewObjectIfNotAvailable)
                    return _generator();
                else
                    LogHelper.Error($"Memory Leak: {Name}: You overflowed the pool! You won't get the performance benefits of the pool. Increase the pool capacity, if the error happens again, you may be forgetting to return the item to the pool.");
            }

            return _generator();
        }

        /// <summary>
        ///* Adiciona um objeto no pool.
        /// </summary>
        [ThreadSafe]
        public void Push(T obj)
        {
            _queue.Push(obj);
        }
    }
}