using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeutronNetwork.Extensions
{
    public static class OthersExt
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
    }
}