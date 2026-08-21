using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class TaskExtensions
    {
        /// <summary>
        /// Observes the task to avoid the task fail silently.
        /// </summary>
        public static void Forget(this Task task, bool logExceptionErrors = true)
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                _ = ForgetAwaited(task, logExceptionErrors: logExceptionErrors, logCanceledTask: false);
            }

            static async Task ForgetAwaited(Task task, bool logExceptionErrors = true, bool logCanceledTask = false)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (TaskCanceledException exception)
                {
                    if (logCanceledTask)
                    {
                        Debug.LogException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (logExceptionErrors)
                        Debug.LogException(exception);
                }
            }
        }
    }
}
