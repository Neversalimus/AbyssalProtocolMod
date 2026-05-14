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
        public bool HasActiveBurst => burstShotsRemaining > 0 && currentBurstTarget != null;

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

        public int BaseMainCooldownTicks => mainModule != null ? mainModule.cooldownTicks : Props.baseCooldownTicks;

        public float ResolvedBasePowerDraw => Mathf.Max(0f, Props.basePowerDraw);
        public float ExtraPowerDraw => SumPassive(module => module.extraPowerDraw) + (auxiliaryModule?.extraPowerDraw ?? 0f) + (mainModule?.extraPowerDraw ?? 0f);
        public float ResolvedModulePowerDraw => FeatureEnabled ? Mathf.Max(0f, ExtraPowerDraw) : 0f;
        public float ResolvedTotalPowerDraw => Mathf.Max(0f, ResolvedBasePowerDraw + ResolvedModulePowerDraw);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            passiveModules ??= new List<ABY_TurretModuleDef>();
            ApplyPowerDraw();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ApplyPowerDraw();
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
                SanitizeLoadedState();
                ApplyPowerDraw();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(60))
            {
                ApplyPowerDraw();
            }

            if (!Operational)
            {
                HaltRuntimeState();
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

        public override void PostDraw()
        {
            base.PostDraw();

            if (parent == null || !parent.Spawned || parent.MapHeld == null || !FeatureEnabled)
            {
                return;
            }

            DrawModuleOverlay(mainModule, new Vector3(0f, 0f, 0.22f), 0.58f, 0.018f);
            DrawModuleOverlay(auxiliaryModule, new Vector3(0.42f, 0f, -0.18f), 0.38f, 0.021f);
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
                    if (passiveModules.Contains(moduleDef))
                    {
                        reason = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_PassiveDuplicate", "This passive module is already installed.");
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

            if (!parent.Spawned || parent.Map == null)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_NoMap", "The chassis must be spawned on a map before modules can be installed.");
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
            if (!FeatureEnabled)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings.");
                return false;
            }

            if (!parent.Spawned || parent.Map == null)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstall_NoMap", "The chassis must be spawned on a map before modules can be installed.");
                return false;
            }

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
            ApplyPowerDraw();
            message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstalledMessage", "Installed {0}.", module.LabelCap);
            return true;
        }

        public bool TryRemoveMainModule(out string message)
        {
            message = null;
            if (!FeatureEnabled)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                return false;
            }

            if (mainModule == null)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemove_Empty", "This slot is already empty.");
                return false;
            }

            ABY_TurretModuleDef module = mainModule;
            if (!ABY_ModularTurretUtility.TryEjectModuleItem(parent, module, out string reason))
            {
                message = reason;
                return false;
            }

            mainModule = null;
            mainCooldownTicks = 0;
            HaltBurst();
            ApplyPowerDraw();
            message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemovedMessage", "Removed {0}.", module.LabelCap);
            return true;
        }

        public bool TryRemoveAuxiliaryModule(out string message)
        {
            message = null;
            if (!FeatureEnabled)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                return false;
            }

            if (auxiliaryModule == null)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemove_Empty", "This slot is already empty.");
                return false;
            }

            ABY_TurretModuleDef module = auxiliaryModule;
            if (!ABY_ModularTurretUtility.TryEjectModuleItem(parent, module, out string reason))
            {
                message = reason;
                return false;
            }

            auxiliaryModule = null;
            auxiliaryCooldownTicks = 0;
            ApplyPowerDraw();
            message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemovedMessage", "Removed {0}.", module.LabelCap);
            return true;
        }

        public bool TryRemovePassiveModule(int index, out string message)
        {
            message = null;
            if (!FeatureEnabled)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                return false;
            }

            if (index < 0 || index >= passiveModules.Count)
            {
                message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemove_Empty", "This slot is already empty.");
                return false;
            }

            ABY_TurretModuleDef module = passiveModules[index];
            if (!ABY_ModularTurretUtility.TryEjectModuleItem(parent, module, out string reason))
            {
                message = reason;
                return false;
            }

            passiveModules.RemoveAt(index);
            ApplyPowerDraw();
            message = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemovedMessage", "Removed {0}.", module.LabelCap);
            return true;
        }

        public void RemoveMainModule()
        {
            TryRemoveMainModule(out _);
        }

        public void RemoveAuxiliaryModule()
        {
            TryRemoveAuxiliaryModule(out _);
        }

        public void RemovePassiveModule(int index)
        {
            TryRemovePassiveModule(index, out _);
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
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectStats", "Range {0} · cooldown {1} · power {2} W", ResolvedRange.ToString("0.0"), ABY_ModularTurretUtility.FormatTicksAsSeconds(ResolvedMainCooldownTicks), ResolvedTotalPowerDraw.ToString("0")));
            }
            else
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInspectNeedsMain", "No main weapon core installed."));
            }

            return string.Join("\n", lines);
        }

        private void DrawModuleOverlay(ABY_TurretModuleDef module, Vector3 localOffset, float size, float altitudeOffset)
        {
            if (module?.thingDef == null)
            {
                return;
            }

            try
            {
                Graphic graphic = module.thingDef.graphicData?.Graphic;
                Material material = graphic?.MatSingle;
                if (material == null)
                {
                    return;
                }

                Vector3 drawPos = parent.DrawPos + localOffset;
                drawPos.y += Mathf.Max(0.001f, altitudeOffset);

                Vector3 scale = new Vector3(Mathf.Max(0.05f, size), 1f, Mathf.Max(0.05f, size));
                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, scale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                if (!AbyssalProtocolMod.Settings.suppressRepeatedWarnings)
                {
                    Log.Warning("[Abyssal Protocol] Modular turret failed to draw module overlay " + module.defName + ": " + ex.GetType().Name + " " + ex.Message);
                }
            }
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
            if (!IsValidLaunchTarget(target, ResolvedRange))
            {
                return;
            }

            currentBurstTarget = target;
            burstShotsRemaining = Mathf.Max(1, mainModule?.burstShotCount ?? 1);
            burstIntervalTicks = 0;
            mainCooldownTicks = ResolvedMainCooldownTicks;
            TickBurst();
        }

        private void TickBurst()
        {
            if (!Operational || !IsValidLaunchTarget(currentBurstTarget, ResolvedRange))
            {
                HaltBurst();
                return;
            }

            if (burstIntervalTicks > 0)
            {
                burstIntervalTicks--;
                return;
            }

            if (mainModule == null || mainModule.projectileDef == null)
            {
                HaltBurst();
                return;
            }

            if (!Launch(mainModule, currentBurstTarget))
            {
                HaltBurst();
                return;
            }

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
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != parent.Map)
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

        private bool IsValidLaunchTarget(Thing target, float range)
        {
            if (target == null || target.Destroyed || !target.Spawned || target.Map != parent.Map || parent.Map == null)
            {
                return false;
            }

            Pawn pawn = target as Pawn;
            if (pawn != null && !ValidTarget(pawn))
            {
                return false;
            }

            if (range > 0f && target.Position.DistanceToSquared(parent.Position) > range * range)
            {
                return false;
            }

            if (!GenSight.LineOfSight(parent.Position, target.Position, parent.Map, true))
            {
                return false;
            }

            return true;
        }

        private bool Launch(ABY_TurretModuleDef module, Thing target)
        {
            float range = module != null && module.range > 0f ? module.range : ResolvedRange;
            if (module?.projectileDef == null || !IsValidLaunchTarget(target, range))
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

        private void ApplyPowerDraw()
        {
            CompPowerTrader powerComp = PowerComp;
            if (powerComp == null)
            {
                return;
            }

            powerComp.PowerOutput = -ResolvedTotalPowerDraw;
        }

        private void SanitizeLoadedState()
        {
            passiveModules ??= new List<ABY_TurretModuleDef>();
            passiveModules.RemoveAll(module => module == null || module.slot != ABY_TurretModuleSlot.Passive || !ModuleAllowed(module));
            passiveModules = passiveModules.Distinct().Take(Mathf.Max(0, Props.passiveSlots)).ToList();

            if (mainModule != null && (mainModule.slot != ABY_TurretModuleSlot.MainWeapon || !ModuleAllowed(mainModule)))
            {
                mainModule = null;
            }

            if (auxiliaryModule != null && (auxiliaryModule.slot != ABY_TurretModuleSlot.Auxiliary || !ModuleAllowed(auxiliaryModule)))
            {
                auxiliaryModule = null;
            }

            mainCooldownTicks = Mathf.Max(0, mainCooldownTicks);
            auxiliaryCooldownTicks = Mathf.Max(0, auxiliaryCooldownTicks);
            burstShotsRemaining = Mathf.Max(0, burstShotsRemaining);
            burstIntervalTicks = Mathf.Max(0, burstIntervalTicks);
            if (burstShotsRemaining <= 0 || currentBurstTarget == null || currentBurstTarget.Destroyed)
            {
                HaltBurst();
            }
        }

        private void HaltRuntimeState()
        {
            HaltBurst();
        }

        private void HaltBurst()
        {
            burstShotsRemaining = 0;
            burstIntervalTicks = 0;
            currentBurstTarget = null;
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
