using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        private static FieldInfo pawnDrawerPawnField;
        private static bool pawnDrawerPawnFieldResolved;

        public static bool IsHoverActive(Pawn pawn)
        {
            return TryGetActiveHoverExtension(pawn, out _);
        }

        public static bool TryGetActiveHover(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            return TryGetActiveHoverExtension(pawn, out extension);
        }

        public static bool TryGetActiveHoverExtension(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            extension = null;

            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.MapHeld == null)
            {
                return false;
            }

            if (pawn.apparel == null)
            {
                return false;
            }

            if (!pawn.Drafted)
            {
                // Current hover armor mode is intentionally drafted-only for both visuals and speed.
                return false;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            if (wornApparel == null || wornApparel.Count == 0)
            {
                return false;
            }

            ABY_HoverArmorExtension best = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                ABY_HoverArmorExtension current = apparel?.def?.GetModExtension<ABY_HoverArmorExtension>();
                if (current == null)
                {
                    continue;
                }

                if (current.draftedOnly && !pawn.Drafted)
                {
                    continue;
                }

                if (current.drawPriority >= bestPriority)
                {
                    best = current;
                    bestPriority = current.drawPriority;
                }
            }

            extension = best;
            return extension != null;
        }

        public static float GetDraftedMoveSpeedBonus(Pawn pawn)
        {
            if (!TryGetActiveHoverExtension(pawn, out ABY_HoverArmorExtension extension) || extension == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, extension.draftedMoveSpeedBonus);
        }

        public static Pawn ResolvePawnFromDrawer(Pawn_DrawTracker drawer)
        {
            if (drawer == null)
            {
                return null;
            }

            try
            {
                FieldInfo field = ResolvePawnDrawerPawnField();
                return field?.GetValue(drawer) as Pawn;
            }
            catch
            {
                return null;
            }
        }

        public static float ComputePawnLiftZ(Pawn pawn, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null)
            {
                return 0f;
            }

            try
            {
                int ticks = SafeTicksGame() + pawn.thingIDNumber * 23;
                float bob = Mathf.Sin(ticks * 0.075f) * Mathf.Max(0f, extension.pawnLiftBobAmplitude);
                return Mathf.Max(0f, extension.pawnLiftZ) + bob;
            }
            catch
            {
                return Mathf.Max(0f, extension.pawnLiftZ);
            }
        }

        public static int SafeTicksGame()
        {
            try
            {
                TickManager tickManager = Find.TickManager;
                if (tickManager != null)
                {
                    return tickManager.TicksGame;
                }
            }
            catch
            {
                // Safe fallback for static constructors / early loading.
            }

            return Environment.TickCount & int.MaxValue;
        }

        private static FieldInfo ResolvePawnDrawerPawnField()
        {
            if (pawnDrawerPawnFieldResolved)
            {
                return pawnDrawerPawnField;
            }

            pawnDrawerPawnFieldResolved = true;
            try
            {
                Type type = typeof(Pawn_DrawTracker);
                pawnDrawerPawnField = type.GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetField("pawnInt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch
            {
                pawnDrawerPawnField = null;
            }

            return pawnDrawerPawnField;
        }
    }
}
