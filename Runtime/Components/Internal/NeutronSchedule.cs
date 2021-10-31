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
        private static readonly NeutronSafeQueueNonAlloc<Action> _tasks = new NeutronSafeQueueNonAlloc<Action>(0);
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
            _tasks.Push(() => Schedule.StartCoroutine(enumerator));
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread).
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static void ScheduleTask(Action action)
        {
            _tasks.Push(action);
        }

        /// <summary>
        ///* Agenda uma função para ser executada no Thread principal(Unity Main Thread).<br/>
        /// </summary>
        /// <param name="func">* A função a ser agendada.</param>
        public static void ScheduleTask<T>(Func<T> func, Action<T> onResult)
        {
            _tasks.Push(() => onResult(func()));
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="enumerator">* A co-rotina a ser agendada.</param>
        public static Task ScheduleTaskAsync(IEnumerator enumerator)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            _tasks.Push(() =>
            {
                Schedule.StartCoroutine(enumerator);
                taskCompletionSource.TrySetResult(true);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="enumerator">* A co-rotina a ser agendada.</param>
        public static Task TryScheduleTaskAsync(TaskCompletionSource<bool> task, IEnumerator enumerator)
        {
            _tasks.Push(() =>
            {
                Schedule.StartCoroutine(enumerator);
            });
            return task.Task;
        }

        /// <summary>
        ///* Agenda uma co-rotina para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="enumerator">* A co-rotina a ser agendada.</param>
        public static Task<T> TryScheduleTaskAsync<T>(TaskCompletionSource<T> task, IEnumerator enumerator)
        {
            _tasks.Push(() =>
            {
                Schedule.StartCoroutine(enumerator);
            });
            return task.Task;
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static Task ScheduleTaskAsync(Action action)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            _tasks.Push(() =>
            {
                action();
                taskCompletionSource.TrySetResult(true);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static Task TryScheduleTaskAsync(Action<TaskCompletionSource<bool>> action)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            _tasks.Push(() =>
            {
                action(taskCompletionSource);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma ação para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="action">* A ação a ser agendada.</param>
        public static Task<T> TryScheduleTaskAsync<T>(Action<TaskCompletionSource<T>> action)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            _tasks.Push(() =>
            {
                action(taskCompletionSource);
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
            _tasks.Push(() =>
            {
                taskCompletionSource.TrySetResult(func());
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///* Agenda uma função para ser executada no Thread principal(Unity Main Thread) de modo assíncrono.
        /// </summary>
        /// <param name="func">* A função a ser agendada.</param>
        public static Task<T> TryScheduleTaskAsync<T>(Func<TaskCompletionSource<T>, T> func)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            _tasks.Push(() =>
            {
                func(taskCompletionSource);
            });
            return taskCompletionSource.Task;
        }

        private void Update()
        {
            // try
            // {
            while (_tasks.Count > 0)
            {
                if (_tasks.TryPull(out Action action))
                    action();
            }
            // }
            // catch (Exception ex)
            // {
            //     LogHelper.Stacktrace(ex);
            // }
        }
    }
}