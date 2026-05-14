using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public sealed class CompAbyssalModularTurret : ThingComp
    {
        private ABY_TurretModuleDef mainModule;
        private ABY_TurretModuleDef auxiliaryModule;
        private List<ABY_TurretModuleDef> passiveModules = new List<ABY_TurretModuleDef>();

        private int mainCooldownTicks;
        private int auxiliaryCooldownTicks;
        private int burstShotsRemaining;
        private int burstIntervalTicks;
        private Thing currentBurstTarget;

        private CompPowerTrader cachedPowerComp;

        public CompProperties_AbyssalModularTurret Props => (CompProperties_AbyssalModularTurret)props;
        public ABY_TurretModuleDef MainModule => mainModule;
        public ABY_TurretModuleDef AuxiliaryModule => auxiliaryModule;
        public IReadOnlyList<ABY_TurretModuleDef> PassiveModules => passiveModules;

        private CompPowerTrader PowerComp => cachedPowerComp ?? (cachedPowerComp = parent.GetComp<CompPowerTrader>());

        public bool FeatureEnabled => ABY_ModularTurretUtility.Enabled;
        public bool HasMainWeapon => mainModule != null && mainModule.projectileDef != null;
        public bool HasAuxiliary => auxiliaryModule != null && auxiliaryModule.projectileDef != null;
        public bool IsPowered => PowerComp == null || PowerComp.PowerOn;
        public bool Operational => FeatureEnabled && parent.Spawned && !parent.Destroyed && IsPowered && parent.Faction == Faction.OfPlayer;

        public float ResolvedRange
        {
            get
            {
                float range = mainModule != null ? mainModule.range : Props.baseRange;
                range += SumPassive(module => module.rangeOffset);
                if (auxiliaryModule != null && auxiliaryModule.slot == ABY_TurretModuleSlot.Auxiliary)
                {
                    range += auxiliaryModule.rangeOffset;
                }

                return Mathf.Max(4f, range);
            }
        }

        public int ResolvedMainCooldownTicks
        {
            get
            {
                int cooldown = mainModule != null ? mainModule.cooldownTicks : Props.baseCooldownTicks;
                float multiplier = Mathf.Max(0.15f, 1f * SumPassiveMultiplier());
                int offset = Mathf.RoundToInt(SumPassive(module => module.cooldownOffsetTicks));
                return Mathf.Max(18, Mathf.RoundToInt(cooldown * multiplier) + offset);
            }
        }

        public float ExtraPowerDraw => SumPassive(module => module.extraPowerDraw) + (auxiliaryModule?.extraPowerDraw ?? 0f) + (mainModule?.extraPowerDraw ?? 0f);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            passiveModules ??= new List<ABY_TurretModuleDef>();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref mainModule, "mainModule");
            Scribe_Defs.Look(ref auxiliaryModule, "auxiliaryModule");
            Scribe_Collections.Look(ref passiveModules, "passiveModules", LookMode.Def);
            Scribe_Values.Look(ref mainCooldownTicks, "mainCooldownTicks", 0);
            Scribe_Values.Look(ref auxiliaryCooldownTicks, "auxiliaryCooldownTicks", 0);
            Scribe_Values.Look(ref burstShotsRemaining, "burstShotsRemaining", 0);
            Scribe_Values.Look(ref burstIntervalTicks, "burstIntervalTicks", 0);
            Scribe_References.Look(ref currentBurstTarget, "currentBurstTarget");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                passiveModules ??= new List<ABY_TurretModuleDef>();
                passiveModules.RemoveAll(module => module == null || module.slot != ABY_TurretModuleSlot.Passive || !ModuleAllowed(module));
                if (mainModule != null && (mainModule.slot != ABY_TurretModuleSlot.MainWeapon || !ModuleAllowed(mainModule)))
                {
                    mainModule = null;
                }

                if (auxiliaryModule != null && (auxiliaryModule.slot != ABY_TurretModuleSlot.Auxiliary || !ModuleAllowed(auxiliaryModule)))
                {
                    auxiliaryModule = null;
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Operational)
            {
                return;
            }

            if (mainCooldownTicks > 0)
            {
                mainCooldownTicks--;
            }

            if (auxiliaryCooldownTicks > 0)
            {
                auxiliaryCooldownTicks--;
            }

            if (burstShotsRemaining > 0)
            {
                TickBurst();
                return;
            }

            if (HasMainWeapon && mainCooldownTicks <= 0 && parent.IsHashIntervalTick(Mathf.Max(1, Props.targetScanIntervalTicks)))
            {
                Thing target = FindTarget(ResolvedRange);
                if (target != null)
                {
                    StartMainBurst(target);
                }
            }

            if (HasAuxiliary && auxiliaryCooldownTicks <= 0 && parent.IsHashIntervalTick(Mathf.Max(1, Props.targetScanIntervalTicks + 11)))
            {
                Thing auxTarget = FindTarget(Mathf.Max(4f, auxiliaryModule.range > 0f ? auxiliaryModule.range : ResolvedRange));
                if (auxTarget != null && Launch(auxiliaryModule, auxTarget))
                {
                    auxiliaryCooldownTicks = Mathf.Max(60, auxiliaryModule.auxiliaryCooldownTicks > 0 ? auxiliaryModule.auxiliaryCooldownTicks : auxiliaryModule.cooldownTicks);
                }
            }
        }

        public bool CanInstall(ABY_TurretModuleDef moduleDef, out string reason)
        {
            reason = null;
            if (moduleDef == null)
            {
                reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_Invalid", "Invalid module.");
                return false;
            }

            if (!moduleDef.CompatibleWith(Props.chassisTag) || !ModuleAllowed(moduleDef))
            {
                reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_Incompatible", "This module is not compatible with this chassis.");
                return false;
            }

            switch (moduleDef.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    if (Props.mainWeaponSlots <= 0)
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_NoMainSlot", "This chassis has no main weapon slot.");
                        return false;
                    }
                    if (mainModule != null)
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_MainOccupied", "The main weapon slot is already occupied.");
                        return false;
                    }
                    return true;

                case ABY_TurretModuleSlot.Auxiliary:
                    if (Props.auxiliarySlots <= 0)
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_NoAuxSlot", "This chassis has no auxiliary slot.");
                        return false;
                    }
                    if (auxiliaryModule != null)
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_AuxOccupied", "The auxiliary slot is already occupied.");
                        return false;
                    }
                    return true;

                case ABY_TurretModuleSlot.Passive:
                    if (passiveModules.Count >= Mathf.Max(0, Props.passiveSlots))
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_PassiveFull", "All passive slots are occupied.");
                        return false;
                    }
                    return true;

                default:
                    reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_InvalidSlot", "Unsupported module slot.");
                    return false;
            }
        }

        public bool TryInstallFromMap(ABY_TurretModuleSlot slot, out string message)
        {
            message = null;
            if (!FeatureEnabled)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings.");
                return false;
            }

            ABY_TurretModuleDef module = ABY_ModularTurretUtility.FindAvailableModuleOnMap(parent.Map, slot, Props.chassisTag);
            if (module == null)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretNoAvailableModule", "No compatible loose module is available on this map.");
                return false;
            }

            return TryInstallSpecificFromMap(module, out message);
        }

        public bool TryInstallSpecificFromMap(ABY_TurretModuleDef module, out string message)
        {
            message = null;
            if (!CanInstall(module, out message))
            {
                return false;
            }

            if (!ABY_ModularTurretUtility.TryConsumeModuleItem(parent.Map, module, parent.Position))
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretNoModuleItem", "The required loose module item could not be found.");
                return false;
            }

            Install(module);
            message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstalledMessage", "Installed {0}.", module.LabelCap);
            return true;
        }

        public void RemoveMainModule()
        {
            if (mainModule == null)
            {
                return;
            }

            ABY_ModularTurretUtility.EjectModuleItem(parent, mainModule);
            mainModule = null;
            mainCooldownTicks = 0;
            burstShotsRemaining = 0;
            currentBurstTarget = null;
        }

        public void RemoveAuxiliaryModule()
        {
            if (auxiliaryModule == null)
            {
                return;
            }

            ABY_ModularTurretUtility.EjectModuleItem(parent, auxiliaryModule);
            auxiliaryModule = null;
            auxiliaryCooldownTicks = 0;
        }

        public void RemovePassiveModule(int index)
        {
            if (index < 0 || index >= passiveModules.Count)
            {
                return;
            }

            ABY_TurretModuleDef module = passiveModules[index];
            ABY_ModularTurretUtility.EjectModuleItem(parent, module);
            passiveModules.RemoveAt(index);
        }

        public override string CompInspectStringExtra()
        {
            List<string> lines = new List<string>();
            if (!FeatureEnabled)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectDisabled", "Modular turret framework disabled in settings."));
                return string.Join("\n", lines);
            }

            string main = mainModule != null ? mainModule.LabelCap : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotEmpty", "empty");
            string aux = auxiliaryModule != null ? auxiliaryModule.LabelCap : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotEmpty", "empty");
            lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectMain", "Main: {0}", main));
            lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectAux", "Auxiliary: {0}", aux));
            lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectPassive", "Passive: {0}/{1}", passiveModules.Count, Props.passiveSlots));
            if (HasMainWeapon)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectStats", "Range {0} · cooldown {1}", ResolvedRange.ToString("0.0"), ABY_ModularTurretUtility.FormatTicksAsSeconds(ResolvedMainCooldownTicks)));
            }
            else
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectNeedsMain", "No main weapon core installed."));
            }

            return string.Join("\n", lines);
        }

        private void Install(ABY_TurretModuleDef module)
        {
            switch (module.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    mainModule = module;
                    mainCooldownTicks = Mathf.Min(mainCooldownTicks, 90);
                    break;
                case ABY_TurretModuleSlot.Auxiliary:
                    auxiliaryModule = module;
                    auxiliaryCooldownTicks = Mathf.Min(auxiliaryCooldownTicks, 120);
                    break;
                case ABY_TurretModuleSlot.Passive:
                    passiveModules.Add(module);
                    break;
            }
        }

        private bool ModuleAllowed(ABY_TurretModuleDef moduleDef)
        {
            if (moduleDef == null || !moduleDef.CompatibleWith(Props.chassisTag))
            {
                return false;
            }

            if (Props.allowedModuleDefNames == null || Props.allowedModuleDefNames.Count == 0)
            {
                return true;
            }

            return Props.allowedModuleDefNames.Contains(moduleDef.defName);
        }

        private void StartMainBurst(Thing target)
        {
            currentBurstTarget = target;
            burstShotsRemaining = Mathf.Max(1, mainModule?.burstShotCount ?? 1);
            burstIntervalTicks = 0;
            mainCooldownTicks = ResolvedMainCooldownTicks;
            TickBurst();
        }

        private void TickBurst()
        {
            if (currentBurstTarget == null || currentBurstTarget.Destroyed || !currentBurstTarget.Spawned || currentBurstTarget.Map != parent.Map)
            {
                burstShotsRemaining = 0;
                currentBurstTarget = null;
                return;
            }

            if (burstIntervalTicks > 0)
            {
                burstIntervalTicks--;
                return;
            }

            if (mainModule == null || mainModule.projectileDef == null)
            {
                burstShotsRemaining = 0;
                currentBurstTarget = null;
                return;
            }

            Launch(mainModule, currentBurstTarget);
            burstShotsRemaining--;
            burstIntervalTicks = Mathf.Max(1, mainModule.ticksBetweenBurstShots);

            if (burstShotsRemaining <= 0)
            {
                currentBurstTarget = null;
            }
        }

        private Thing FindTarget(float range)
        {
            Map map = parent.Map;
            if (map?.mapPawns == null)
            {
                return null;
            }

            float rangeSquared = range * range;
            Thing bestTarget = null;
            float bestScore = -999999f;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!ValidTarget(pawn))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(parent.Position);
                if (distanceSquared > rangeSquared)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(parent.Position, pawn.Position, map, true))
                {
                    continue;
                }

                float score = 10000f - distanceSquared;
                if (pawn.Downed)
                {
                    score -= 4000f;
                }

                if (bestTarget == null || score > bestScore)
                {
                    bestTarget = pawn;
                    bestScore = score;
                }
            }

            return bestTarget;
        }

        private bool ValidTarget(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Dead || pawn.Map != parent.Map)
            {
                return false;
            }

            if (pawn.Faction == null || parent.Faction == null)
            {
                return false;
            }

            if (!pawn.Faction.HostileTo(parent.Faction))
            {
                return false;
            }

            return true;
        }

        private bool Launch(ABY_TurretModuleDef module, Thing target)
        {
            if (module?.projectileDef == null || target == null || parent.Map == null)
            {
                return false;
            }

            Projectile projectile = GenSpawn.Spawn(module.projectileDef, parent.Position, parent.Map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return false;
            }

            try
            {
                LocalTargetInfo targetInfo = new LocalTargetInfo(target);
                projectile.Launch(parent, parent.DrawPos, targetInfo, targetInfo, ProjectileHitFlags.IntendedTarget, false, null, null);
                module.soundCast?.PlayOneShot(SoundInfo.InMap(new TargetInfo(parent.Position, parent.Map, false), MaintenanceType.None));
                return true;
            }
            catch (Exception ex)
            {
                if (!projectile.Destroyed)
                {
                    projectile.Destroy(DestroyMode.Vanish);
                }

                if (!AbyssalProtocolMod.Settings.suppressRepeatedWarnings)
                {
                    Log.Warning("[Abyssal Protocol] Modular turret failed to launch projectile " + module.projectileDef.defName + ": " + ex.GetType().Name + " " + ex.Message);
                }

                return false;
            }
        }

        private float SumPassive(Func<ABY_TurretModuleDef, float> selector)
        {
            float total = 0f;
            for (int i = 0; i < passiveModules.Count; i++)
            {
                ABY_TurretModuleDef module = passiveModules[i];
                if (module != null)
                {
                    total += selector(module);
                }
            }

            return total;
        }

        private float SumPassiveMultiplier()
        {
            float multiplier = 1f;
            for (int i = 0; i < passiveModules.Count; i++)
            {
                ABY_TurretModuleDef module = passiveModules[i];
                if (module != null && module.cooldownMultiplier > 0f && Math.Abs(module.cooldownMultiplier - 1f) > 0.0001f)
                {
                    multiplier *= module.cooldownMultiplier;
                }
            }

            if (auxiliaryModule != null && auxiliaryModule.cooldownMultiplier > 0f && Math.Abs(auxiliaryModule.cooldownMultiplier - 1f) > 0.0001f)
            {
                multiplier *= auxiliaryModule.cooldownMultiplier;
            }

            return multiplier;
        }
    }
}
