using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Wrappers;
using System;

//* Não use ConcurrentQueue, por algum motivo essa porra tem vazamento de memória no metódo Enqueue(), aloca demais, e o GC Congela a unity quando inicia a limpeza.
public class NeutronPool<T>
{
    //* Inicializa um queue para o pool.
    private readonly NeutronQueue<T> _queue = new NeutronQueue<T>();
    //* Objeto que irá gerar novas instâncias quando necessário.
    private readonly Func<T> _generator;
    //* Objeto para sincronizar a inserção e remoção de objetos em multiplos threads.
    private readonly object _lock = new object();

    //* Obtém a quantidade de objetos no pool de threads.
    [ThreadSafe]
    public int Count {
        get {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
    /// <summary>
    ///* Obtém a capacidade máxima de objetos permitida no pool.
    /// </summary>
    public int Capacity { get; }
    /// <summary>
    ///* Define se a capacidade é aumentada conforme o necessário.
    /// </summary>
    public bool Resizable { get; }

    /// <summary>
    /// Inicializa um novo pool do tipo especificado em T.
    /// </summary>
    public NeutronPool(Func<T> generator, int capacity, bool resizable)
    {
        this._generator = generator;
        Capacity = capacity;
        Resizable = resizable;
    }

    /// <summary>
    ///* Obtém um objeto disponível do pool de objetos.
    /// </summary>
    [ThreadSafe]
    public T Pull()
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
                return _queue.Dequeue();
            else
            {
                if (Resizable)
                    return _generator();
                else
                    LogHelper.Error("You overflowed the pool! You won't get the performance benefits of the pool, it increases capacity.");
            }
            return _generator();
        }
    }

    /// <summary>
    ///* Adiciona um objeto no pool.
    /// </summary>
    [ThreadSafe]
    public void Push(T obj)
    {
        lock (_lock)
        {
            if (_queue.Count < Capacity)
                _queue.Enqueue(obj);
        }
    }
}