using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class CompProperties_AbyssalReloadable : CompProperties
    {
        public int maxCharges = 1;
        public ThingDef ammoDef;
        public int ammoCountPerCharge = 1;
        public int ammoCountToRefill = 1;
        public int baseReloadTicks = 60;
        public int verbToUse;
        public bool displayGizmoWhileUndrafted;
        public string chargeNoun = "charge";
        public bool destroyOnEmpty;
        public SoundDef soundReload;

        public CompProperties_AbyssalReloadable()
        {
            compClass = typeof(CompAbyssalReloadable);
        }
    }

    public class CompAbyssalReloadable : ThingComp
    {
        private int remainingCharges = -1;

        public CompProperties_AbyssalReloadable Props => (CompProperties_AbyssalReloadable)props;

        public int MaxCharges => Math.Max(1, Props.maxCharges);

        public int RemainingCharges
        {
            get
            {
                if (remainingCharges < 0)
                {
                    remainingCharges = MaxCharges;
                }
                return Mathf.Clamp(remainingCharges, 0, MaxCharges);
            }
        }

        public bool IsFull => RemainingCharges >= MaxCharges;
        public bool IsEmpty => RemainingCharges <= 0;
        public int ChargesMissing => Math.Max(0, MaxCharges - RemainingCharges);
        public int AmmoNeededToFull => ChargesMissing * Math.Max(1, Props.ammoCountPerCharge);

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (remainingCharges < 0)
            {
                remainingCharges = MaxCharges;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingCharges, "remainingCharges", -1);
        }

        public bool CanFireNow(out string reason)
        {
            if (!IsEmpty)
            {
                reason = null;
                return true;
            }

            string noun = ChargeNounPluralized();
            reason = "Out of " + noun + ".";
            return false;
        }

        public void NotifyShotFired(Pawn wearer)
        {
            remainingCharges = Math.Max(0, RemainingCharges - 1);
            if (IsEmpty && Props.destroyOnEmpty && parent != null && !parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
                return;
            }

            if (wearer != null && wearer.IsColonistPlayerControlled && IsEmpty)
            {
                Messages.Message(parent.LabelCap + " is empty and needs " + ChargeNounPluralized() + ".", parent, MessageTypeDefOf.NeutralEvent, false);
            }
        }

        public override string CompInspectStringExtra()
        {
            return ChargeNounLabel() + ": " + RemainingCharges + " / " + MaxCharges;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn wearer = ResolveWearer();
            if (wearer == null || !wearer.IsColonistPlayerControlled || wearer.Dead)
            {
                yield break;
            }

            if (!Props.displayGizmoWhileUndrafted && !wearer.Drafted)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = "Reload " + parent.LabelNoCount,
                defaultDesc = "Consume " + (Props.ammoDef?.label ?? "ammo") + " to refill this weapon.",
                action = delegate
                {
                    TryReloadFromWearerOrGround(wearer, true);
                }
            };

            if (IsFull)
            {
                command.Disable("Already fully loaded.");
            }
            else if (CountAvailableAmmo(wearer) < Math.Max(1, Props.ammoCountPerCharge))
            {
                command.Disable("No " + (Props.ammoDef?.label ?? "ammo") + " available.");
            }

            yield return command;
        }

        public bool TryAutoReload(Pawn wearer)
        {
            if (wearer == null || IsFull)
            {
                return false;
            }

            if (CountAvailableAmmo(wearer) < Math.Max(1, Props.ammoCountPerCharge))
            {
                return false;
            }

            return TryReloadFromWearerOrGround(wearer, false);
        }

        public bool TryReloadFromWearerOrGround(Pawn wearer, bool sendMessages)
        {
            if (wearer == null || (wearer.MapHeld == null && wearer.inventory == null) || Props.ammoDef == null)
            {
                return false;
            }

            int loaded = 0;
            int perCharge = Math.Max(1, Props.ammoCountPerCharge);
            while (!IsFull && TryConsumeAmmo(wearer, perCharge))
            {
                remainingCharges = Math.Min(MaxCharges, RemainingCharges + 1);
                loaded++;
            }

            if (loaded > 0)
            {
                if (wearer.MapHeld != null)
                {
                    Props.soundReload?.PlayOneShot(new TargetInfo(wearer.PositionHeld, wearer.MapHeld));
                }

                if (sendMessages)
                {
                    Messages.Message(parent.LabelCap + " reloaded (" + RemainingCharges + " / " + MaxCharges + ").", wearer, MessageTypeDefOf.TaskCompletion, false);
                }
                return true;
            }

            if (sendMessages)
            {
                Messages.Message("No " + (Props.ammoDef?.label ?? "ammo") + " available to reload " + parent.LabelNoCount + ".", wearer, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        public int CountAvailableAmmo(Pawn wearer)
        {
            if (wearer == null || Props.ammoDef == null)
            {
                return 0;
            }

            int count = 0;
            if (wearer.inventory != null)
            {
                foreach (Thing thing in wearer.inventory.innerContainer)
                {
                    if (thing.def == Props.ammoDef)
                    {
                        count += thing.stackCount;
                    }
                }
            }

            if (wearer.Spawned && wearer.MapHeld != null)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(wearer.PositionHeld, 1.9f, true))
                {
                    if (!cell.InBounds(wearer.MapHeld))
                    {
                        continue;
                    }

                    List<Thing> thingList = cell.GetThingList(wearer.MapHeld);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing thing = thingList[i];
                        if (thing.def == Props.ammoDef)
                        {
                            count += thing.stackCount;
                        }
                    }
                }
            }

            return count;
        }

        private bool TryConsumeAmmo(Pawn wearer, int count)
        {
            if (Props.ammoDef == null || wearer == null)
            {
                return false;
            }

            int remaining = count;
            if (wearer.inventory != null)
            {
                remaining = ConsumeFromThings(wearer.inventory.innerContainer, remaining);
            }

            if (remaining <= 0)
            {
                return true;
            }

            if (!wearer.Spawned || wearer.MapHeld == null)
            {
                return false;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(wearer.PositionHeld, 1.9f, true))
            {
                if (!cell.InBounds(wearer.MapHeld))
                {
                    continue;
                }

                List<Thing> thingList = cell.GetThingList(wearer.MapHeld);
                remaining = ConsumeFromThings(thingList, remaining);
                if (remaining <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int ConsumeFromThings(IEnumerable<Thing> sourceThings, int remaining)
        {
            List<Thing> matching = new List<Thing>();
            foreach (Thing thing in sourceThings)
            {
                if (thing != null && thing.def == Props.ammoDef && thing.stackCount > 0)
                {
                    matching.Add(thing);
                }
            }

            for (int i = 0; i < matching.Count && remaining > 0; i++)
            {
                Thing thing = matching[i];
                int take = Math.Min(thing.stackCount, remaining);
                if (take <= 0)
                {
                    continue;
                }

                Thing taken = thing.SplitOff(take);
                taken.Destroy(DestroyMode.Vanish);
                remaining -= take;
            }

            return remaining;
        }

        private Pawn ResolveWearer()
        {
            object current = parent?.ParentHolder;
            for (int i = 0; i < 8 && current != null; i++)
            {
                if (current is Pawn pawn)
                {
                    return pawn;
                }

                object directPawn = GetMemberValue(current, "pawn") ?? GetMemberValue(current, "Pawn");
                if (directPawn is Pawn holderPawn)
                {
                    return holderPawn;
                }

                if (current is Thing thing)
                {
                    current = thing.ParentHolder;
                    continue;
                }

                object next = GetMemberValue(current, "Owner") ?? GetMemberValue(current, "owner") ?? GetMemberValue(current, "ParentHolder") ?? GetMemberValue(current, "parentHolder");
                current = next;
            }

            return null;
        }

        private static object GetMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(instance, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            return null;
        }

        private string ChargeNounLabel()
        {
            string noun = string.IsNullOrWhiteSpace(Props.chargeNoun) ? "charge" : Props.chargeNoun.Trim();
            return noun.CapitalizeFirst();
        }

        private string ChargeNounPluralized()
        {
            string noun = string.IsNullOrWhiteSpace(Props.chargeNoun) ? "charge" : Props.chargeNoun.Trim();
            return noun.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? noun : noun + "s";
        }
    }
}
