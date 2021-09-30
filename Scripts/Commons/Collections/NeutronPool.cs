using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Wrappers;
using System;

namespace NeutronNetwork
{
    //* Não use ConcurrentQueue, por algum motivo essa porra tem vazamento de memória no metódo Enqueue(), aloca demais, e o GC Congela a unity quando inicia a limpeza.
    public class NeutronPool<T>
    {
        //* Inicializa um queue para o pool.
        private readonly NeutronSafeQueueNonAlloc<T> _queue;
        //* Objeto que irá gerar novas instâncias quando necessário.
        private readonly Func<T> _generator;
        /// <summary>
        ///* Define se a capacidade é aumentada conforme o necessário.
        /// </summary>
        public bool Resizable {
            get;
        }
        /// <summary>
        ///* Nome do pool de objetos.
        /// </summary>
        public string Name {
            get;
        }
        /// <summary>
        ///* Quantidade de objetos no pool.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Inicializa um novo pool do tipo especificado em T.
        /// </summary>
        public NeutronPool(Func<T> generator, int capacity, bool resizable, string name)
        {
            _generator = generator;
            //***************************************************
            _queue = new NeutronSafeQueueNonAlloc<T>(capacity);
            //***************************************************
            Resizable = resizable;
            Name = name;
        }

        /// <summary>
        ///* Obtém um objeto disponível do pool de objetos.
        /// </summary>
        [ThreadSafe]
        public T Pull()
        {
            if (_queue.TryDequeue(out T item))
                return item;
            else
            {
                if (Resizable)
                    return _generator();
                else
                    LogHelper.Error($"{Name}: You overflowed the pool! You won't get the performance benefits of the pool, it increases capacity.");
            }
            return _generator();
        }

        /// <summary>
        ///* Adiciona um objeto no pool.
        /// </summary>
        [ThreadSafe]
        public void Push(T obj)
        {
            _queue.Enqueue(obj);
        }
    }
}