using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronSchedule : MarkupBehaviour
    {
        private enum DispatcherMode
        {
            OnlyUpdate,
            UpdateAndWhile,
        }

        [SerializeField]
        [Box("Schedule Options")]
        [InfoBox("Unity does not allow its API to be called from a thread other than the main one.", EInfoBoxType.Warning)]
        [InfoBox("This class performs Unity's native actions on the Unity's main thread.")]
        [InfoBox("Dispatch actions sparingly so as not to cause bottlenecks in the action queue.", EInfoBoxType.Warning)]
        [InfoBox("There are ways to avoid using dispatch see documentation.", EInfoBoxType.Warning)]
        [InfoBox("This option defines how actions should be performed on the Unity Thread.")]
        private DispatcherMode _dispatcherMode = DispatcherMode.UpdateAndWhile; // The type of dispatcher to use.
        [InfoBox("The higher the value, the higher the CPU usage, depending on the amount of data queued per second.", EInfoBoxType.Warning)]
        [InfoBox("This option defines how much data will be dequeued per call.")]
        [SerializeField] [Range(1, 10)] private int _multiplier = 1; // The multiplier for the amount of data to dequeue per call.

        #region Singleton
        public static NeutronSchedule Schedule { get; private set; } // The singleton instance of the schedule.
        #endregion

        #region Collections
        private static readonly NeutronSafeQueueNonAlloc<Action> _tasks = new NeutronSafeQueueNonAlloc<Action>(0); // The queue of actions to perform.
        #endregion

        private object m_lockUpdate = new object(); // The lock for the update method.
        private object m_lockFixedUpdate = new object(); // The lock for the fixed update method.
        private Action _dispatchOnUpdate; // The action to perform on the update method.
        private Action _dispatchOnFixedUpdate; // The action to perform on the fixed update method.

        private void Awake()
        {
            Schedule = this; // Set the singleton instance.
        }

        /// <summary>
        /// Schedules an coroutine to be performed on the Unity's main thread.
        /// </summary>
        /// <param name="enumerator"></param>
        public static void ScheduleTask(IEnumerator enumerator)
        {
            _tasks.Push(() => Schedule.StartCoroutine(enumerator));
        }

        /// <summary>
        /// Schedules an action to be performed on the Unity's main thread.
        /// </summary>
        /// <param name="action"></param>
        public static void ScheduleTask(Action action)
        {
            _tasks.Push(action);
        }

        /// <summary>
        /// Schedules an function to be performed on the Unity's main thread and returns a result.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="onResult"></param>
        /// <typeparam name="T"></typeparam>
        public static void ScheduleTask<T>(Func<T> func, Action<T> onResult)
        {
            _tasks.Push(() => onResult(func()));
        }

        /// <summary>
        /// Schedules an coroutine to be performed on the Unity's main thread.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
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
        /// Schedules an coroutine to be performed on the Unity's main thread with a custom task completion source.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public static Task TryScheduleTaskAsync(TaskCompletionSource<bool> task, IEnumerator enumerator)
        {
            _tasks.Push(() =>
            {
                Schedule.StartCoroutine(enumerator);
            });
            return task.Task;
        }

        /// <summary>
        /// Schedules an coroutine to be performed on the Unity's main thread with a custom task completion source.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public static Task<T> TryScheduleTaskAsync<T>(TaskCompletionSource<T> task, IEnumerator enumerator)
        {
            _tasks.Push(() =>
            {
                Schedule.StartCoroutine(enumerator);
            });
            return task.Task;
        }

        /// <summary>
        /// Schedules an action to be performed on the Unity's main thread.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <returns></returns>
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
        /// Schedules an action to be performed on the Unity's main thread with a custom task completion source.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <returns></returns>
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
        /// Schedules an action to be performed on the Unity's main thread with a custom task completion source.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <returns></returns>
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
        /// Schedules an func to be performed on the Unity's main thread.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <returns></returns>
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
        /// Schedules an func to be performed on the Unity's main thread with a custom task completion source.
        /// This method creates garbage(Task) on the heap(The pressure on the GC is greater, use with caution).
        /// </summary>
        /// <returns></returns>
        public static Task<T> TryScheduleTaskAsync<T>(Func<TaskCompletionSource<T>, T> func)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            _tasks.Push(() =>
            {
                func(taskCompletionSource);
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Fast Invoke: Invokes the action on Update in the next frame, no task scheduling is required.
        /// </summary>
        /// <param name="action"></param>
        public static void RunOnUpdate(Action action)
        {
            lock (Schedule.m_lockUpdate)
            {
                // lock to prevent race condition.

                Schedule._dispatchOnUpdate = action; // Set the action.
            }
        }

        /// <summary>
        /// Fast Invoke: Invokes the action on Fixed Update in the next frame, no task scheduling is required.
        /// </summary>
        /// <param name="action"></param>
        public static void RunOnFixedUpdate(Action action)
        {
            lock (Schedule.m_lockFixedUpdate)
            {
                // lock to prevent race condition.

                Schedule._dispatchOnFixedUpdate = action; // Set the action.
            }
        }

        private void Update()
        {
            try
            {
                lock (m_lockUpdate)
                {
                    if (_dispatchOnUpdate != null)
                    {
                        _dispatchOnUpdate(); // Invoke the action.
                        _dispatchOnUpdate = null; // Clear the action.
                    }
                }

                while (_tasks.Count > 0)
                {
                    if (_tasks.TryPull(out Action action))
                        action(); // Invoke the action.
                }
            }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        private void FixedUpdate()
        {
            try
            {
                lock (m_lockFixedUpdate)
                {
                    if (_dispatchOnFixedUpdate != null)
                    {
                        _dispatchOnFixedUpdate();
                        _dispatchOnFixedUpdate = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }
    }
}