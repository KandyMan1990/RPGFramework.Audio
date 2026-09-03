using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Music_Sample
{
    public static class TaskExtensions
    {
        public static void FireAndForget(this Task task, Action<Exception> onException = null, bool continueOnMainThread = false)
        {
            _ = HandleTaskAsync(task, onException, continueOnMainThread);
        }

        private static async Task HandleTaskAsync(Task task, Action<Exception> onException, bool continueOnMainThread)
        {
            try
            {
                await task.ConfigureAwait(!continueOnMainThread);
            }
            catch (Exception ex)
            {
                try
                {
                    onException?.Invoke(ex);
                }
                catch (Exception handlerEx)
                {
                    Debug.LogError($"FireAndForget exception handler threw: {handlerEx}");
                }

                if (onException == null)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}