using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Heavy T5 multilance verb.
    ///
    /// The weapon intentionally does not launch four physical projectiles. It starts one
    /// transient beam-sequence thing that handles rail-charge presentation and four controlled
    /// damage pulses. This keeps the weapon visually distinctive without per-tick beam damage,
    /// per-shot map scans, or projectile spam.
    /// </summary>
    public class Verb_CrownReactorMultilance : Verb_Shoot
    {
        private static ThingDef beamSequenceDef;

        protected override bool TryCastShot()
        {
            if (caster == null || caster.Map == null || verbProps == null)
            {
                return false;
            }

            LocalTargetInfo target = currentTarget;
            if (!target.IsValid || !target.Cell.IsValid || !target.Cell.InBounds(caster.Map))
            {
                return false;
            }

            ThingDef sequenceDef = BeamSequenceDef;
            if (sequenceDef == null)
            {
                return false;
            }

            Thing_CrownReactorBeamSequence sequence = ThingMaker.MakeThing(sequenceDef) as Thing_CrownReactorBeamSequence;
            if (sequence == null)
            {
                return false;
            }

            sequence.Initialize(caster, EquipmentSource, target, verbProps.defaultProjectile);
            GenSpawn.Spawn(sequence, target.Cell, caster.Map);
            return true;
        }

        private static ThingDef BeamSequenceDef => beamSequenceDef ?? (beamSequenceDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_CrownReactorBeamSequence"));
    }
}
