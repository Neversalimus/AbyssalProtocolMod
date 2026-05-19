using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_MonsterInfoCardIconNormalizer
    {
        private const float TargetDrawSize = 2.35f;
        private const float MinIconScale = 0.34f;
        private const float MaxIconScale = 0.86f;
        private static readonly Vector2 UnifiedIconOffset = Vector2.zero;

        static ABY_MonsterInfoCardIconNormalizer()
        {
            LongEventHandler.ExecuteWhenFinished(Apply);
        }

        private static void Apply()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!ShouldNormalize(def))
                {
                    continue;
                }

                float referenceSize = ResolveReferenceDrawSize(def);
                float scale = Mathf.Clamp(TargetDrawSize / referenceSize, MinIconScale, MaxIconScale);

                if (IsHeavyBossProfile(def))
                {
                    scale = Mathf.Min(scale, 0.58f);
                }

                def.uiIconScale = scale;
                def.uiIconOffset = UnifiedIconOffset;
            }
        }

        private static bool ShouldNormalize(ThingDef def)
        {
            if (def == null || def.category != ThingCategory.Pawn || def.race == null)
            {
                return false;
            }

            if (def.comps == null || def.comps.Count == 0)
            {
                return false;
            }

            return def.comps.Any(comp => comp is CompProperties_AbyssalAutoHostile);
        }

        private static float ResolveReferenceDrawSize(ThingDef def)
        {
            if (def.graphicData != null)
            {
                float maxAxis = Mathf.Max(def.graphicData.drawSize.x, def.graphicData.drawSize.y);
                if (maxAxis > 0.01f)
                {
                    return maxAxis;
                }
            }

            float bodySize = def.race?.baseBodySize ?? 1f;
            return Mathf.Max(1f, bodySize * 2f);
        }

        private static bool IsHeavyBossProfile(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.defName == "ABY_ReactorSaint")
            {
                return true;
            }

            float maxAxis = def.graphicData == null ? 0f : Mathf.Max(def.graphicData.drawSize.x, def.graphicData.drawSize.y);
            return maxAxis >= 5.25f;
        }
    }
}
