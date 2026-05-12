using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice heart presentation.
    ///
    /// The earlier implementation drew several large rotating halo/ring layers around the heart.
    /// After the Dominion Sepulcher redesign those circles fought the new industrial platform art,
    /// so this utility now keeps the heart readable and machine-like: the building texture carries
    /// the setpiece identity, while runtime pulses are limited to tiny electrical feedback.
    /// </summary>
    public static class DominionSliceHeartSetpieceVfxUtility
    {
        public static void DrawHeartSetpiece(Vector3 heartPos, Map map, MapComponent_DominionSliceEncounter encounter, int seed)
        {
            // Intentionally disabled. The redesigned heart platform texture is the setpiece now.
            // Keeping this method as a no-op preserves all call sites while removing the old
            // magic-circle halo stack from the Dominion Slice.
        }

        public static void SpawnHeartbeatPulse(Vector3 heartPos, Map map, bool exposed)
        {
            if (map == null)
            {
                return;
            }

            // Small feedback only: no floor rings, crown rings, exposed-core circles, or shield halos.
            float glowScale = exposed ? 0.82f : 0.42f;
            FleckMaker.ThrowLightningGlow(heartPos, map, glowScale);

            if (exposed)
            {
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(0.18f, 0f, 0.10f), map);
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(-0.16f, 0f, -0.12f), map);
            }
        }
    }
}
