using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_CrownspikeRailBolt : Bullet
    {
        private bool preImpactFlashDone;

        protected override void Tick()
        {
            if (!preImpactFlashDone && Spawned && Map != null)
            {
                preImpactFlashDone = true;
                Vector3 source = ResolveSourcePosition();
                Vector3 forwardPoint = ExactPosition;
                if ((forwardPoint - source).MagnitudeHorizontal() > 0.15f)
                {
                    CrownspikeRailVfxUtility.SpawnRailDischarge(source, forwardPoint, Map, null, false);
                }
            }

            base.Tick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Vector3 source = ResolveSourcePosition();

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            CrownspikeRailVfxUtility.SpawnRailDischarge(source, impactPosition, impactMap, hitThing, blockedByShield);
        }

        private Vector3 ResolveSourcePosition()
        {
            Thing launcher = Launcher;
            if (launcher != null && !launcher.Destroyed)
            {
                return launcher.DrawPos;
            }

            return ExactPosition;
        }
    }
}
