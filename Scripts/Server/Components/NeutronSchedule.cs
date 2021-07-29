using NeutronNetwork.Constants;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronSchedule : MonoBehaviour
    {
        #region Singleton
        public static NeutronSchedule Schedule { get; set; }
        #endregion

        #region Collections
        private static NeutronSafeQueue<Action> _tasks = new NeutronSafeQueue<Action>();
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
        ///* Agenda uma a��o para ser executada no Thread principal(Unity Main Thread).
        /// </summary>
        /// <param name="action">* A a��o a ser agendada.</param>
        public static void ScheduleTask(Action action)
        {
            _tasks.Enqueue(action);
        }

        /// <summary>
        ///* Agenda uma fun��o para ser executada no Thread principal(Unity Main Thread).<br/>
        ///* Este met�do n�o � recomendado, use a vers�o ass�ncrona.
        /// </summary>
        /// <param name="func">* A fun��o a ser agendada.</param>
        [Obsolete("Utilize a vers�o ass�ncrona desta sobrecarga!")]
        public static T ScheduleTask<T>(Func<T> func)
        {
            T result = default;
            _tasks.Enqueue(() => result = func.Invoke());
            return result;
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread) de modo ass�ncrono.
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
        ///* Agenda uma a��o para ser executada no Thread principal(Unity Main Thread) de modo ass�ncrono.
        /// </summary>
        /// <param name="action">* A a��o a ser agendada.</param>
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
        ///* Agenda uma fun��o para ser executada no Thread principal(Unity Main Thread) de modo ass�ncrono.
        /// </summary>
        /// <param name="func">* A fun��o a ser agendada.</param>
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
                for (int i = 0; i < NeutronMain.Settings.GlobalSettings.ActionsProcessedPerFrame && _tasks.Count > 0; i++)
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