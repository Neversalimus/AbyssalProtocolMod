using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class DeathActionWorker_ABY_ChoirEngineBurst : DeathActionWorker
    {
        private const float BurstRadius = 7.2f;
        private const float TurretEmpDamage = 6.0f;
        private const float MechEmpDamage = 6.5f;

        public override void PawnDied(Corpse corpse, Lord prevLord)
        {

            Pawn pawn = corpse?.InnerPawn;
            Map map = corpse?.MapHeld;
            if (pawn == null || map == null)
            {
                return;
            }

            IntVec3 center = corpse.PositionHeld;
            FleckMaker.ThrowLightningGlow(corpse.DrawPos, map, 2.8f);
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 1.9f);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", center, map);

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(center, map, BurstRadius, true))
            {
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                if (thing is Building_Turret turret && turret.Faction != null && pawn.Faction != null && ABY_FactionHostilityUtility.SafeHostileTo(pawn.Faction, turret.Faction))
                {
                    turret.TakeDamage(new DamageInfo(DamageDefOf.EMP, TurretEmpDamage, 0f, -1f, pawn));
                }
                else if (thing is Pawn mech && mech.RaceProps != null && mech.RaceProps.IsMechanoid && mech.Faction != null && pawn.Faction != null && ABY_FactionHostilityUtility.SafeHostileTo(pawn.Faction, mech.Faction))
                {
                    mech.TakeDamage(new DamageInfo(DamageDefOf.EMP, MechEmpDamage, 0f, -1f, pawn));
                }
                // Avoid applying death-burst EMP directly to ordinary power buildings. Turrets and
                // hostile mechanoids are still affected above, but generators/batteries/conduits should
                // not be transiently stunned or forced into misleading power-grid states.
            }
        }
    }
}
