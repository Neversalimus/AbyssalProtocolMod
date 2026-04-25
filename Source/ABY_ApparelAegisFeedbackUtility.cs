using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ApparelAegisFeedbackUtility
    {
        private static readonly Dictionary<string, int> LastFeedbackTickByKey = new Dictionary<string, int>();

        public static void TriggerHit(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (!CanShow(pawn, ext))
            {
                return;
            }

            if (!CanPassCooldown(pawn, "hit", ext.MinorFeedbackCooldownTicksSafe))
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.hitFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.hitSoundDefName, pawn.PositionHeld, pawn.MapHeld);
        }

        public static void TriggerCollapse(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (!CanShow(pawn, ext))
            {
                return;
            }

            if (!CanPassCooldown(pawn, "collapse", ext.MajorFeedbackCooldownTicksSafe))
            {
                return;
            }

            float scale = Mathf.Max(0.1f, ext.breakFlashScale);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, scale);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, scale * 0.58f));
            ABY_ApparelAegisUtility.PlaySound(ext.breakSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            TryRegisterScreenPulse(pawn.MapHeld, ext.collapsePulseStrength, ext);
            if (ext.showAegisCombatText)
            {
                TryThrowText(pawn, ABY_ApparelAegisUtility.TranslateOrFallback(ext.collapseTextKey, "AEGIS COLLAPSE"), new Color(1f, 0.35f, 0.20f, 1f));
            }
        }

        public static void TriggerRestore(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (!CanShow(pawn, ext))
            {
                return;
            }

            if (!CanPassCooldown(pawn, "restore", ext.MajorFeedbackCooldownTicksSafe))
            {
                return;
            }

            float scale = Mathf.Max(0.1f, ext.restoreFlashScale);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, scale);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, scale * 0.42f));
            ABY_ApparelAegisUtility.PlaySound(ext.restoreSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            TryRegisterScreenPulse(pawn.MapHeld, ext.restorePulseStrength, ext);
            if (ext.showAegisCombatText)
            {
                TryThrowText(pawn, ABY_ApparelAegisUtility.TranslateOrFallback(ext.restoreTextKey, "AEGIS ONLINE"), new Color(0.72f, 0.95f, 1f, 1f));
            }
        }

        private static bool CanShow(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            return pawn?.MapHeld != null && pawn.PositionHeld.IsValid && ext != null;
        }

        private static bool CanPassCooldown(Pawn pawn, string eventId, int cooldownTicks)
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            string key = (pawn?.thingIDNumber ?? 0) + ":" + (eventId ?? string.Empty);
            if (LastFeedbackTickByKey.TryGetValue(key, out int lastTick) && tick - lastTick < cooldownTicks)
            {
                return false;
            }

            LastFeedbackTickByKey[key] = tick;
            return true;
        }

        private static void TryRegisterScreenPulse(Map map, float strength, DefModExtension_ABY_ApparelAegis ext)
        {
            if (map == null || ext == null || !ext.showAegisScreenPulse || strength <= 0.001f || Current.Game == null)
            {
                return;
            }

            try
            {
                AbyssalBossScreenFXGameComponent component = Current.Game.GetComponent<AbyssalBossScreenFXGameComponent>();
                component?.RegisterRitualPulse(map, Mathf.Clamp01(strength));
            }
            catch
            {
            }
        }

        private static void TryThrowText(Pawn pawn, string text, Color color)
        {
            if (pawn?.MapHeld == null || text.NullOrEmpty())
            {
                return;
            }

            try
            {
                // Reflection keeps this tolerant across minor RimWorld overload changes.
                MethodInfo[] methods = typeof(MoteMaker).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != "ThrowText")
                    {
                        continue;
                    }

                    ParameterInfo[] p = method.GetParameters();
                    if (p.Length < 3)
                    {
                        continue;
                    }

                    object[] args = new object[p.Length];
                    args[0] = pawn.DrawPos;
                    args[1] = pawn.MapHeld;
                    args[2] = text;
                    for (int j = 3; j < p.Length; j++)
                    {
                        Type t = p[j].ParameterType;
                        if (t == typeof(Color))
                        {
                            args[j] = color;
                        }
                        else if (t == typeof(float))
                        {
                            args[j] = 1.4f;
                        }
                        else if (t == typeof(bool))
                        {
                            args[j] = false;
                        }
                        else if (t.IsValueType)
                        {
                            args[j] = Activator.CreateInstance(t);
                        }
                        else
                        {
                            args[j] = null;
                        }
                    }

                    method.Invoke(null, args);
                    return;
                }
            }
            catch
            {
            }
        }
    }
}
