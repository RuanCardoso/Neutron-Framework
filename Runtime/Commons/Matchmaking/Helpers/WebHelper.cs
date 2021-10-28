using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace NeutronNetwork.Helpers
{
    public static class WebHelper
    {
        /// <summary>
        ///* Post a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to post the request.</param>
        /// <param name="formData">The form data to post the request.</param>
        /// <param name="onAwake">The callback to invoke when the request is initialized.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static void Post(string url, Dictionary<string, string> formData, Action<UnityWebRequest> onAwake, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Post(url, formData);
                onAwake.Invoke(request);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }

        /// <summary>
        ///* Post a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to post the request.</param>
        /// <param name="formData">The form data to post the request.</param>
        /// <param name="onAwake">The callback to invoke when the request is initialized.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static Task PostAsync(string url, Dictionary<string, string> formData, Action<UnityWebRequest> onAwake, Action<UnityWebRequest> onResult)
        {
            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Post(url, formData);
                onAwake.Invoke(request);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
                task.TrySetResult(true);
            }
            NeutronSchedule.ScheduleTask(Request());
            return task.Task;
        }

        /// <summary>
        ///* Post a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to post the request.</param>
        /// <param name="formData">The form data to post the request.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static void Post(string url, Dictionary<string, string> formData, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Post(url, formData);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }

        /// <summary>
        ///* Post a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to post the request.</param>
        /// <param name="formData">The form data to post the request.</param>
        /// <param name="onAwake">The callback to invoke when the request is initialized.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static Task PostAsync(string url, Dictionary<string, string> formData, Action<UnityWebRequest> onResult)
        {
            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Post(url, formData);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
                task.TrySetResult(true);
            }
            NeutronSchedule.ScheduleTask(Request());
            return task.Task;
        }

        /// <summary>
        ///* Get a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to get the request.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static void Get(string url, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }

        /// <summary>
        ///* Get a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to get the request.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static Task GetAsync(string url, Action<UnityWebRequest> onResult)
        {
            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
                task.TrySetResult(true);
            }
            NeutronSchedule.ScheduleTask(Request());
            return task.Task;
        }

        /// <summary>
        ///* Get a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to get the request.</param>
        /// <param name="onAwake">The callback to invoke when the request is initialized.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static void Get(string url, Action<UnityWebRequest> onAwake, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                onAwake.Invoke(request);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }

        /// <summary>
        ///* Get a request to the specified url.<br/>
        /// </summary>
        /// <param name="url">The url to get the request.</param>
        /// <param name="onAwake">The callback to invoke when the request is initialized.</param>
        /// <param name="onResult">The callback to invoke when the request is finished.</param>
        public static Task GetAsync(string url, Action<UnityWebRequest> onAwake, Action<UnityWebRequest> onResult)
        {
            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                onAwake.Invoke(request);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
                task.TrySetResult(true);
            }
            NeutronSchedule.ScheduleTask(Request());
            return task.Task;
        }
    }
}