using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Verb_ShootCrownspikeRail : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn != null && casterPawn.Spawned && casterPawn.Map != null)
            {
                Vector3 source = casterPawn.DrawPos;
                source.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                CrownspikeRailVfxUtility.SpawnChargeAt(source, casterPawn.Map);
            }

            return base.TryCastShot();
        }
    }
}
