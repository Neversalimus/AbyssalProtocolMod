using System.Diagnostics;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DevToolUtility
    {
        public static bool IsDebugToolActiveForInput()
        {
            return Prefs.DevMode && IsDebugToolStackActive();
        }

        public static bool IsDebugToolActiveOrExecuting()
        {
            return Prefs.DevMode && IsDebugToolStackActive();
        }

        public static bool IsRecentDebugToolAction(int graceTicks = 3)
        {
            return IsDebugToolActiveOrExecuting();
        }

        private static bool IsDebugToolStackActive()
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            try
            {
                StackTrace trace = new StackTrace(false);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    System.Reflection.MethodBase method = trace.GetFrame(i)?.GetMethod();
                    string declaringType = method?.DeclaringType?.FullName;
                    string methodName = method?.Name;
                    if (IsDebugToolName(declaringType) || IsDebugToolName(methodName))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool IsDebugToolName(string value)
        {
            if (value.NullOrEmpty())
            {
                return false;
            }

            return value.Contains("DebugTools")
                || value.Contains("DebugTool")
                || value.Contains("DebugActions")
                || value.Contains("DebugAction")
                || value.Contains("Dialog_Debug")
                || value.Contains("EditWindow_Debug")
                || value.Contains("DevTool")
                || value.Contains("DevelopmentalStage");
        }
    }
}
