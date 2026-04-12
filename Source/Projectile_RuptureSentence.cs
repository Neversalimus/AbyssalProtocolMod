using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_RuptureSentence : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 cell = Position;
            Pawn pawn = hitThing as Pawn;

            if (pawn == null && map != null && cell.IsValid && cell.InBounds(map))
            {
                pawn = cell.GetFirstPawn(map);
            }

            if (pawn != null)
            {
                RuptureCrownUtility.TryApplyMark(pawn);
            }

            if (map != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", cell, map);

                ThingDef mote = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloCore");
                if (mote != null)
                {
                    MoteMaker.MakeStaticMote(ExactPosition + new Vector3(0f, 0f, 0.05f), map, mote, 0.70f);
                }
            }

            Destroy(DestroyMode.Vanish);
        }
    }
}
