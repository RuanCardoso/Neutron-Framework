using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeutronNetwork.Extensions
{
    public static class Ext
    {
        public static async Task<bool> RunWithTimeout(this Task task, TimeSpan timeSpan)
        {
            using (CancellationTokenSource timeoutToken = new CancellationTokenSource())
            {
                var whenTask = await Task.WhenAny(task, Task.Delay(timeSpan, timeoutToken.Token));
                if (whenTask == task)
                {
                    timeoutToken.Cancel();
                    await task;
                    return true;
                }
                else
                    return false;
            }
        }

        public static async Task<T> RunWithTimeout<T>(this Task<T> task, TimeSpan timeSpan)
        {
            using (CancellationTokenSource timeoutToken = new CancellationTokenSource())
            {
                var whenTask = await Task.WhenAny(task, Task.Delay(timeSpan, timeoutToken.Token));
                if (whenTask == task)
                {
                    timeoutToken.Cancel();
                    return await task;
                }
                else
                    throw new Exception("Timeout!");
            }
        }

        public static JToken RemoveFields(this JToken token, string[] fields)
        {
            JContainer container = token as JContainer;
            if (container == null)
                return token;

            List<JToken> removeList = new List<JToken>();
            foreach (JToken el in container.Children())
            {
                JProperty p = el as JProperty;
                if (p != null && fields.Contains(p.Name))
                    removeList.Add(el);
                el.RemoveFields(fields);
            }

            foreach (JToken el in removeList)
                el.Remove();
            return token;
        }

#if !UNITY_SERVER || UNITY_EDITOR
        public static string Bold(this string str) => "<b>" + str + "</b>";
        public static string Color(this string str, string clr) => string.Format("<color={0}>{1}</color>", clr, str);
        public static string Italic(this string str) => "<i>" + str + "</i>";
        public static string Size(this string str, int size) => string.Format("<size={0}>{1}</size>", size, str);
#elif UNITY_SERVER
        public static string Bold(this string str) => str;
        public static string Color(this string str, string clr) => str;
        public static string Italic(this string str) => str;
        public static string Size(this string str, int size) => str;
#endif
    }
}