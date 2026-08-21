using System.Diagnostics;
using System;
using UnityEditor;
using Unity.Multiplayer.PlayMode.Common.Editor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local.Android
{
namespace Unity.Android.Logcat
{
    internal abstract class AndroidLogcatBase
    {
        protected AndroidBridgeHelper.ADB m_ADB;
        protected string m_LogPrintFormat;
        protected string m_DeviceID;
        protected Action<string> m_LogCallbackAction;

        internal AndroidLogcatBase(AndroidBridgeHelper.ADB adb, string logPrintFormat, string device, Action<string> logCallbackAction)
        {
            m_ADB = adb;
            m_LogPrintFormat = logPrintFormat;
            m_DeviceID = device;
            m_LogCallbackAction = logCallbackAction;
        }

        public abstract void Start(string logPath);
        public abstract void Stop();
        public abstract void Kill();
        public abstract bool HasExited { get; }
    }

    internal class AndroidLogcat : AndroidLogcatBase
    {
        public Process m_LogcatProcess;

        internal AndroidLogcat(AndroidBridgeHelper.ADB adb, string logPrintFormat, string device, Action<string> logCallbackAction)
            : base(adb, logPrintFormat, device, logCallbackAction)
        {
        }

        private string LogcatArguments(string logPath)
        {
            var filterArg = "Unity";
#if UNITY_EDITOR_WIN
            return  string.Format("-s {0} logcat | Select-String {1} | Select-String {2} > \"{3}\"",
                m_DeviceID, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), filterArg, logPath);
#else
            return string.Format("-s {0} logcat | grep {1} | grep -i {2} > \"{3}\"",
                m_DeviceID, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) , filterArg, logPath);
#endif
        }

        public Process GetLogcatProcess()
        {
            return m_LogcatProcess;
        }

        public override void Start(string logPath)
        {
            var arguments = LogcatArguments(logPath);
            var executablePath = m_ADB.GetADBPath();
            var streamLogsArgument = string.Empty;
            DebugUtils.Trace($"Starting logcat: {m_ADB.GetADBPath()} {arguments}");
            m_LogcatProcess = new Process();
#if UNITY_EDITOR_WIN
            {
                m_LogcatProcess.StartInfo.FileName = "powershell.exe";
                m_LogcatProcess.StartInfo.Arguments =  $"\"& '{executablePath}' {arguments}\"";
            }
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            {
                streamLogsArgument = " > \\\"" + logPath + "\\\"";
                m_LogcatProcess.StartInfo.FileName = "/bin/bash";
                m_LogcatProcess.StartInfo.Arguments = $"-c \"exec {executablePath} {arguments}{streamLogsArgument}\"";
            }
#endif
            m_LogcatProcess.StartInfo.RedirectStandardError = true;
            m_LogcatProcess.StartInfo.RedirectStandardOutput = true;
            m_LogcatProcess.StartInfo.UseShellExecute = false;
            m_LogcatProcess.StartInfo.CreateNoWindow = true;
            m_LogcatProcess.OutputDataReceived += OutputDataReceived;
            m_LogcatProcess.ErrorDataReceived += OutputDataReceived;
            m_LogcatProcess.Start();

            m_LogcatProcess.BeginOutputReadLine();
            m_LogcatProcess.BeginErrorReadLine();
        }

        public override void Stop()
        {
            if (m_LogcatProcess != null && !m_LogcatProcess.HasExited)
                 Kill();

            m_LogcatProcess = null;
        }

        public override void Kill()
        {
            // NOTE: DONT CALL CLOSE, or ADB process will stay alive all the time
            DebugUtils.Trace("Stopping logcat  " + m_LogcatProcess.Id);
            m_LogcatProcess.Kill();
            m_LogcatProcess.WaitForExit(100); // possibly with a timeout
        }

        public override bool HasExited
        {
            get
            {
                return m_LogcatProcess.HasExited;
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_LogCallbackAction(e.Data);
        }
    }
}
}
