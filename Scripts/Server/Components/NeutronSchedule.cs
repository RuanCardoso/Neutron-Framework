using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Wrappers;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronSchedule : MonoBehaviour
    {
        #region Singleton
        public static NeutronSchedule Schedule { get; set; }
        #endregion

        #region Collections
        private static readonly NeutronSafeQueue<Action> _tasks = new NeutronSafeQueue<Action>();
        #endregion

        private void Awake()
        {
            Schedule = this;
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread).
        /// </summary>
        /// <param name="enumerator">* A co-rotina a ser agendada.</param>
        public static void ScheduleTask(IEnumerator enumerator)
        {
            _tasks.Enqueue(() => Schedule.StartCoroutine(enumerator));
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread).
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static void ScheduleTask(Action action)
        {
            _tasks.Enqueue(action);
        }

        /// <summary>
        ///* Agenda uma função para ser executada no Thread principal(Unity Main Thread).<br/>
        ///* Este metódo não é recomendado, use a versão assíncrona.
        /// </summary>
        /// <param name="func">* A função a ser agendada.</param>
        [Obsolete("Use the asynchronous version of this overload!")]
        public static T ScheduleTask<T>(Func<T> func)
        {
            T result = default;
            _tasks.Enqueue(() => result = func.Invoke());
            return result;
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="enumerator">* A co-rotina a ser agendada.</param>
        public static Task ScheduleTaskAsync(IEnumerator enumerator)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            _tasks.Enqueue(() =>
            {
                Schedule.StartCoroutine(enumerator);
                taskCompletionSource.TrySetResult(true);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static Task ScheduleTaskAsync(Action action)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            _tasks.Enqueue(() =>
            {
                action.Invoke();
                taskCompletionSource.TrySetResult(true);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma função para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="func">* A função a ser agendada.</param>
        public static Task<T> ScheduleTaskAsync<T>(Func<T> func)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            _tasks.Enqueue(() =>
            {
                taskCompletionSource.TrySetResult(func.Invoke());
            });
            return taskCompletionSource.Task;
        }

        private void Update()
        {
            try
            {
                while (_tasks.Count > 0)
                {
                    if (_tasks.TryDequeue(out Action action))
                        action.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogHelper.StackTrace(ex);
            }
        }
    }
}