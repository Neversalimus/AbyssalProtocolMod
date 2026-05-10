using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        public static bool IsHoverActive(Pawn pawn)
        {
            return TryGetActiveHoverExtension(pawn, out _);
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
    }
}
