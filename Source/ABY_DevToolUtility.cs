using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DevToolUtility
    {
        private static int lastDetectedDebugToolTick = -999999;
        private static int cachedActiveToolTick = -999999;
        private static bool cachedActiveToolResult;

        public static bool IsDebugToolActiveForInput()
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            if (HasActiveDebugToolField())
            {
                MarkDebugToolDetected();
                return true;
            }

            return false;
        }

        public static bool IsDebugToolActiveOrExecuting()
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            if (HasActiveDebugToolField() || IsDebugCallStackActive())
            {
                MarkDebugToolDetected();
                return true;
            }

            return IsRecentDebugToolAction(3);
        }

        public static bool IsRecentDebugToolAction(int graceTicks = 3)
        {
            if (!Prefs.DevMode || Find.TickManager == null)
            {
                return false;
            }

            return Find.TickManager.TicksGame - lastDetectedDebugToolTick <= Math.Max(0, graceTicks);
        }

        private static void MarkDebugToolDetected()
        {
            if (Find.TickManager != null)
            {
                lastDetectedDebugToolTick = Find.TickManager.TicksGame;
            }
        }

        private static bool HasActiveDebugToolField()
        {
            int tick = Find.TickManager?.TicksGame ?? -1;
            if (tick == cachedActiveToolTick)
            {
                return cachedActiveToolResult;
            }

            bool result = false;
            try
            {
                if (HasActiveDebugToolFieldOnType(AccessTools.TypeByName("LudeonTK.DebugTools")))
                {
                    result = true;
                }
                else if (HasActiveDebugToolFieldOnType(AccessTools.TypeByName("Verse.DebugTools") ?? typeof(Log).Assembly.GetType("Verse.DebugTools")))
                {
                    result = true;
                }
            }
            catch
            {
                result = false;
            }

            cachedActiveToolTick = tick;
            cachedActiveToolResult = result;
            return result;
        }

        private static bool HasActiveDebugToolFieldOnType(Type debugToolsType)
        {
            if (debugToolsType == null)
            {
                return false;
            }

            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // RimWorld 1.6/LudeonTK stores the currently armed map debug tool in curTool.
            string[] directNames = { "curTool", "currentTool", "selectedTool", "activeTool" };
            for (int i = 0; i < directNames.Length; i++)
            {
                FieldInfo directField = AccessTools.Field(debugToolsType, directNames[i]);
                if (directField != null && IsLiveDebugToolValue(SafeGetFieldValue(directField)))
                {
                    return true;
                }

                PropertyInfo directProperty = AccessTools.Property(debugToolsType, directNames[i]);
                if (directProperty != null && directProperty.CanRead && directProperty.GetIndexParameters().Length == 0 && IsLiveDebugToolValue(SafeGetPropertyValue(directProperty)))
                {
                    return true;
                }
            }

            FieldInfo[] fields = debugToolsType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null || !LooksLikeDebugToolSlot(field.Name, field.FieldType))
                {
                    continue;
                }

                if (IsLiveDebugToolValue(SafeGetFieldValue(field)))
                {
                    return true;
                }
            }

            PropertyInfo[] props = debugToolsType.GetProperties(flags);
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];
                if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length != 0 || !LooksLikeDebugToolSlot(prop.Name, prop.PropertyType))
                {
                    continue;
                }

                if (IsLiveDebugToolValue(SafeGetPropertyValue(prop)))
                {
                    return true;
                }
            }

            return false;
        }

        private static object SafeGetFieldValue(FieldInfo field)
        {
            try
            {
                return field.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        private static object SafeGetPropertyValue(PropertyInfo property)
        {
            try
            {
                return property.GetValue(null, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeDebugToolSlot(string name, Type valueType)
        {
            string loweredName = name ?? string.Empty;
            loweredName = loweredName.ToLowerInvariant();
            string loweredType = valueType?.FullName ?? valueType?.Name ?? string.Empty;
            loweredType = loweredType.ToLowerInvariant();

            return loweredName.Contains("tool")
                || loweredName.Contains("debug")
                || loweredType.Contains("debugtool")
                || loweredType.Contains("debugaction")
                || loweredType.Contains("ludeontk");
        }

        private static bool IsLiveDebugToolValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is string || value is bool || value is int || value is float)
            {
                return false;
            }

            Type type = value.GetType();
            string typeName = type.FullName ?? type.Name ?? string.Empty;
            if (typeName.IndexOf("DebugTool", StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.IndexOf("DebugAction", StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.IndexOf("LudeonTK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // Some RimWorld builds wrap debug tools in delegates/actions. If the active slot contains a
            // delegate while DevMode is on, treat it as armed debug input rather than letting expanded boss
            // selection steal the click.
            return value is Delegate;
        }

        private static bool IsDebugCallStackActive()
        {
            try
            {
                StackTrace trace = new StackTrace(false);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    MethodBase method = trace.GetFrame(i)?.GetMethod();
                    Type type = method?.DeclaringType;
                    if (type == null)
                    {
                        continue;
                    }

                    string fullName = type.FullName ?? type.Name ?? string.Empty;
                    if (fullName.IndexOf("DebugTools", StringComparison.OrdinalIgnoreCase) >= 0
                        || fullName.IndexOf("DebugTool", StringComparison.OrdinalIgnoreCase) >= 0
                        || fullName.IndexOf("DebugAction", StringComparison.OrdinalIgnoreCase) >= 0
                        || fullName.IndexOf("Dialog_Debug", StringComparison.OrdinalIgnoreCase) >= 0
                        || fullName.IndexOf("LudeonTK", StringComparison.OrdinalIgnoreCase) >= 0)
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
    }
}
