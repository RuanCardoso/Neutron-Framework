using System;
using System.Threading.Tasks;

// It refers to this link : https://github.com/Microsoft/xbox-live-unity-plugin/blob/master/Assets/Xbox%20Live/Scripts/UnityTaskExtensions.cs

namespace Asyncoroutine
{
    public static class TaskYieldInstructionExtension
    {
        public static TaskYieldInstruction AsCoroutine(this Task task)
        {
            if (task == null)
            {
                throw new NullReferenceException();
            }

            return new TaskYieldInstruction(task);
        }

        public static TaskYieldInstruction<T> AsCoroutine<T>(this Task<T> task)
        {
            if (task == null)
            {
                throw new NullReferenceException();
            }

            return new TaskYieldInstruction<T>(task);
        }
    }
}