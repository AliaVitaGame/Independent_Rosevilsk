using System;
using Unity.Services.Multiplay.Authoring.Core.Threading;

namespace Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Scheduler
{
    class DispatcherToMainThread : IDispatchToMainThread
    {
        public void DispatchAction(Action action,
            string filePath = null,
            string member = null,
            int num = 0)
        {
            Shared.Threading.Sync.RunNextUpdateOnMain(action, filePath, member, num);
        }
    }
}
