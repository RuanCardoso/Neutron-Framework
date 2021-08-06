using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Wrappers;
using System;

//* N�o use ConcurrentQueue, por algum motivo essa porra tem vazamento de mem�ria no met�do Enqueue(), aloca demais, e o GC Congela a unity quando inicia a limpeza.
public class NeutronPool<T>
{
    //* Inicializa um queue para o pool.
    private readonly NeutronQueue<T> _queue = new NeutronQueue<T>();
    //* Objeto que ir� gerar novas inst�ncias quando necess�rio.
    private readonly Func<T> _generator;
    //* Objeto para sincronizar a inser��o e remo��o de objetos em multiplos threads.
    private readonly object _lock = new object();

    //* Obt�m a quantidade de objetos no pool de threads.
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
    ///* Obt�m a capacidade m�xima de objetos permitida no pool.
    /// </summary>
    public int Capacity { get; }
    /// <summary>
    ///* Define se a capacidade � aumentada conforme o necess�rio.
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
    ///* Obt�m um objeto dispon�vel do pool de objetos.
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