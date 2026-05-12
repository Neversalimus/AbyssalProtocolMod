using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice anchor presentation.
    ///
    /// Anchor identity used to be communicated by broad animated ritual circles. The redesign moves
    /// that identity into the industrial anchor platform textures themselves. Runtime VFX is limited
    /// to small electrical feedback so the anchors read as machinery instead of spell circles.
    /// </summary>
    public static class DominionSliceAnchorIdentityVfxUtility
    {
        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, bool activeEncounter, bool anchorfallActive, int seed)
        {
            // Intentionally disabled: no large anchor zone circles/glyphs.
        }

        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, int seed, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            // Intentionally disabled: no large anchor zone circles/glyphs.
        }

        public static void SpawnAnchorPulse(Vector3 drawLoc, Map map, DominionSliceAnchorRole role)
        {
            if (map == null)
            {
                return;
            }

            float glowScale;
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    glowScale = 0.72f;
                    break;
                case DominionSliceAnchorRole.Law:
                    glowScale = 0.80f;
                    break;
                default:
                    glowScale = 0.64f;
                    break;
            }

            FleckMaker.ThrowLightningGlow(drawLoc, map, glowScale);
            FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(Rand.Range(-0.18f, 0.18f), 0f, Rand.Range(-0.18f, 0.18f)), map);
        }
    }
}
