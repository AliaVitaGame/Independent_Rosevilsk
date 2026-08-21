using System;
using System.Runtime.CompilerServices;

namespace Unity.Services.Multiplay.Authoring.Core.Threading
{
    interface IDispatchToMainThread
    {
        void DispatchAction(
            Action action,
            [CallerFilePath] string filePath = null,
            [CallerMemberName] string member = null,
            [CallerLineNumber] int num = 0);
    }
}
