using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DevToolUtility
    {
        public static bool IsDebugToolActiveOrExecuting()
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            return HasActiveDebugToolField() || IsDebugCallStackActive();
        }

        private static bool HasActiveDebugToolField()
        {
            try
            {
                Type debugToolsType = AccessTools.TypeByName("Verse.DebugTools") ?? typeof(Log).Assembly.GetType("Verse.DebugTools");
                if (debugToolsType == null)
                {
                    return false;
                }

                BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo[] fields = debugToolsType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field == null)
                    {
                        continue;
                    }

                    if (!LooksLikeDebugToolSlot(field.Name, field.FieldType))
                    {
                        continue;
                    }

                    object value = null;
                    try
                    {
                        value = field.GetValue(null);
                    }
                    catch
                    {
                    }

                    if (IsLiveDebugToolValue(value))
                    {
                        return true;
                    }
                }

                PropertyInfo[] props = debugToolsType.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo prop = props[i];
                    if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    if (!LooksLikeDebugToolSlot(prop.Name, prop.PropertyType))
                    {
                        continue;
                    }

                    object value = null;
                    try
                    {
                        value = prop.GetValue(null, null);
                    }
                    catch
                    {
                    }

                    if (IsLiveDebugToolValue(value))
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

        private static bool LooksLikeDebugToolSlot(string name, Type valueType)
        {
            string loweredName = name ?? string.Empty;
            loweredName = loweredName.ToLowerInvariant();
            string loweredType = valueType?.FullName ?? valueType?.Name ?? string.Empty;
            loweredType = loweredType.ToLowerInvariant();

            return loweredName.Contains("tool")
                || loweredName.Contains("debug")
                || loweredType.Contains("debugtool")
                || loweredType.Contains("debugaction");
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
            return typeName.IndexOf("DebugTool", StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.IndexOf("DebugAction", StringComparison.OrdinalIgnoreCase) >= 0
                || typeName.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0;
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
                        || fullName.IndexOf("Dialog_Debug", StringComparison.OrdinalIgnoreCase) >= 0)
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
