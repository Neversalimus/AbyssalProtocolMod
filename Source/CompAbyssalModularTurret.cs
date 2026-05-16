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
        private static readonly Color AuxiliaryRangeColor = new Color(0.62f, 0.34f, 1f, 0.62f);
        private static readonly Color AuxiliaryMinRangeColor = new Color(0.82f, 0.46f, 1f, 0.48f);
        private static readonly Color MainMinRangeColor = new Color(1f, 0.55f, 0.28f, 0.50f);
        private static readonly Dictionary<string, Material> AnimatedOverlayMaterialCache = new Dictionary<string, Material>();

        private ABY_TurretModuleDef mainModule;
        private ABY_TurretModuleDef auxiliaryModule;
        private List<ABY_TurretModuleDef> passiveModules = new List<ABY_TurretModuleDef>();

        private int mainCooldownTicks;
        private int auxiliaryCooldownTicks;
        private int burstShotsRemaining;
        private int burstIntervalTicks;
        private Thing currentBurstTarget;

        private int mainChargeTicksRemaining;
        private int mainChargeTicksTotal;
        private int mainDischargeTicksRemaining;
        private int mainDischargeTicksTotal;
        private int mainResidualTicksRemaining;
        private int mainResidualTicksTotal;

        private float mainAimAngle;
        private float auxiliaryAimAngle;

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

        public int ResolvedMainChargeTicks
        {
            get
            {
                if (mainModule == null || mainModule.chargeTicks <= 0)
                {
                    return 0;
                }

                float multiplier = Mathf.Max(0.15f, SumPassiveMultiplier());
                int offset = Mathf.RoundToInt(SumPassive(module => module.cooldownOffsetTicks) * 0.5f);
                return Mathf.Max(12, Mathf.RoundToInt(mainModule.chargeTicks * multiplier) + offset);
            }
        }

        public float ResolvedMainMinRange => mainModule != null ? Mathf.Max(0f, mainModule.minRange) : 0f;

        public float ResolvedAuxiliaryRange
        {
            get
            {
                if (auxiliaryModule == null)
                {
                    return 0f;
                }

                return Mathf.Max(4f, auxiliaryModule.range > 0f ? auxiliaryModule.range : ResolvedRange);
            }
        }

        public float ResolvedAuxiliaryMinRange => auxiliaryModule != null ? Mathf.Max(0f, auxiliaryModule.minRange) : 0f;

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
            Scribe_Values.Look(ref mainChargeTicksRemaining, "mainChargeTicksRemaining", 0);
            Scribe_Values.Look(ref mainChargeTicksTotal, "mainChargeTicksTotal", 0);
            Scribe_Values.Look(ref mainDischargeTicksRemaining, "mainDischargeTicksRemaining", 0);
            Scribe_Values.Look(ref mainDischargeTicksTotal, "mainDischargeTicksTotal", 0);
            Scribe_Values.Look(ref mainResidualTicksRemaining, "mainResidualTicksRemaining", 0);
            Scribe_Values.Look(ref mainResidualTicksTotal, "mainResidualTicksTotal", 0);
            Scribe_Values.Look(ref mainAimAngle, "mainAimAngle", 0f);
            Scribe_Values.Look(ref auxiliaryAimAngle, "auxiliaryAimAngle", 0f);
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

            if (parent.IsHashIntervalTick(15))
            {
                UpdateVisualAimAngles();
            }

            TickTimedVisualOverlays();

            if (mainCooldownTicks > 0)
            {
                mainCooldownTicks--;
            }

            if (auxiliaryCooldownTicks > 0)
            {
                auxiliaryCooldownTicks--;
            }

            if (mainChargeTicksRemaining > 0)
            {
                TickMainCharge();
                return;
            }

            if (burstShotsRemaining > 0)
            {
                TickBurst();
                return;
            }

            if (HasMainWeapon && mainCooldownTicks <= 0 && parent.IsHashIntervalTick(Mathf.Max(1, Props.targetScanIntervalTicks)))
            {
                Thing target = FindTarget(ResolvedRange, ResolvedMainMinRange);
                if (target != null)
                {
                    StartMainAttack(target);
                }
            }

            if (HasAuxiliary && auxiliaryCooldownTicks <= 0 && parent.IsHashIntervalTick(Mathf.Max(1, Props.targetScanIntervalTicks + 11)))
            {
                Thing auxTarget = FindTarget(ResolvedAuxiliaryRange, ResolvedAuxiliaryMinRange);
                if (auxTarget != null)
                {
                    auxiliaryAimAngle = AngleToTarget(auxTarget);
                    if (Launch(auxiliaryModule, auxTarget))
                    {
                        auxiliaryCooldownTicks = Mathf.Max(60, auxiliaryModule.auxiliaryCooldownTicks > 0 ? auxiliaryModule.auxiliaryCooldownTicks : auxiliaryModule.cooldownTicks);
                    }
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

            DrawWeaponOverlay(mainModule, mainAimAngle, false);
            DrawWeaponOverlay(auxiliaryModule, auxiliaryAimAngle, false);
            DrawMainAnimatedOverlays();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();

            if (parent == null || !parent.Spawned || parent.MapHeld == null || !FeatureEnabled)
            {
                return;
            }

            if (HasMainWeapon)
            {
                DrawMainRangeRings(ResolvedMainMinRange, ResolvedRange);
            }

            if (HasAuxiliary)
            {
                DrawAuxiliaryRangeRings(ResolvedAuxiliaryMinRange, ResolvedAuxiliaryRange);
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
            HaltCharge();
            mainDischargeTicksRemaining = 0;
            mainDischargeTicksTotal = 0;
            mainResidualTicksRemaining = 0;
            mainResidualTicksTotal = 0;
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

        private void DrawMainRangeRings(float minRange, float maxRange)
        {
            DrawRangeRing(maxRange, null);
            if (minRange > 0.1f && minRange < maxRange - 0.1f)
            {
                DrawRangeRing(minRange, MainMinRangeColor);
            }
        }

        private void DrawAuxiliaryRangeRings(float minRange, float maxRange)
        {
            DrawRangeRing(maxRange, AuxiliaryRangeColor);
            if (minRange > 0.1f && minRange < maxRange - 0.1f)
            {
                DrawRangeRing(minRange, AuxiliaryMinRangeColor);
            }
        }

        private void DrawRangeRing(float range, Color? color)
        {
            if (range <= 0.1f)
            {
                return;
            }

            if (color.HasValue)
            {
                GenDraw.DrawRadiusRing(parent.Position, range, color.Value, null);
            }
            else
            {
                GenDraw.DrawRadiusRing(parent.Position, range);
            }
        }

        private void DrawWeaponOverlay(ABY_TurretModuleDef module, float aimAngle, bool forceVisible)
        {
            if (module == null || (!forceVisible && !module.overlayVisibleWhenDisabled && !FeatureEnabled))
            {
                return;
            }

            if (!module.IsWeaponLike || !module.HasOverlay)
            {
                return;
            }

            try
            {
                Material material = ABY_ModularTurretUtility.GetOverlayMaterial(module.overlayTexturePath);
                if (material == null)
                {
                    return;
                }

                float angle = module.overlayRotatesToTarget ? aimAngle : 0f;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 drawPos = ResolveOverlayPlaneCenter(module, rotation);
                drawPos.y += Mathf.Max(0.001f, module.overlayAltitudeOffset);

                float size = Mathf.Max(0.08f, module.overlayDrawSize);
                Vector3 scale = new Vector3(size, 1f, size);
                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rotation, scale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                if (!AbyssalProtocolMod.Settings.suppressRepeatedWarnings)
                {
                    Log.Warning("[Abyssal Protocol] Modular turret failed to draw rotating overlay " + module.defName + ": " + ex.GetType().Name + " " + ex.Message);
                }
            }
        }

        private void DrawMainAnimatedOverlays()
        {
            if (mainModule == null || !mainModule.IsWeaponLike || !mainModule.HasOverlay)
            {
                return;
            }

            if (mainChargeTicksRemaining > 0 && !mainModule.chargeOverlayFramePathPrefix.NullOrEmpty())
            {
                int elapsed = Mathf.Max(0, mainChargeTicksTotal - mainChargeTicksRemaining);
                DrawAnimatedModuleOverlay(
                    mainModule,
                    mainModule.chargeOverlayFramePathPrefix,
                    mainModule.chargeOverlayFrameCount,
                    mainModule.chargeOverlayTicksPerFrame,
                    elapsed,
                    mainAimAngle,
                    mainModule.chargeOverlayAltitudeOffset);
            }

            if (mainDischargeTicksRemaining > 0 && !mainModule.dischargeOverlayFramePathPrefix.NullOrEmpty())
            {
                int elapsed = Mathf.Max(0, mainDischargeTicksTotal - mainDischargeTicksRemaining);
                DrawAnimatedModuleOverlay(
                    mainModule,
                    mainModule.dischargeOverlayFramePathPrefix,
                    mainModule.dischargeOverlayFrameCount,
                    mainModule.dischargeOverlayTicksPerFrame,
                    elapsed,
                    mainAimAngle,
                    mainModule.dischargeOverlayAltitudeOffset);
            }

            if (mainResidualTicksRemaining > 0 && !mainModule.residualOverlayFramePathPrefix.NullOrEmpty())
            {
                int elapsed = Mathf.Max(0, mainResidualTicksTotal - mainResidualTicksRemaining);
                DrawAnimatedModuleOverlay(
                    mainModule,
                    mainModule.residualOverlayFramePathPrefix,
                    mainModule.residualOverlayFrameCount,
                    mainModule.residualOverlayTicksPerFrame,
                    elapsed,
                    mainAimAngle,
                    mainModule.residualOverlayAltitudeOffset);
            }
        }

        private void DrawAnimatedModuleOverlay(
            ABY_TurretModuleDef module,
            string framePathPrefix,
            int frameCount,
            int ticksPerFrame,
            int elapsedTicks,
            float aimAngle,
            float altitudeOffset)
        {
            if (module == null || framePathPrefix.NullOrEmpty())
            {
                return;
            }

            try
            {
                int safeFrameCount = Mathf.Max(1, frameCount);
                int safeTicksPerFrame = Mathf.Max(1, ticksPerFrame);
                int frameIndex = Mathf.Clamp(elapsedTicks / safeTicksPerFrame, 0, safeFrameCount - 1);
                string path = framePathPrefix + (frameIndex + 1).ToString("00");
                Material material = GetAnimatedOverlayMaterial(path);
                if (material == null)
                {
                    return;
                }

                float angle = module.overlayRotatesToTarget ? aimAngle : 0f;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 drawPos = ResolveOverlayPlaneCenter(module, rotation);
                drawPos.y += Mathf.Max(0.001f, altitudeOffset);

                float size = Mathf.Max(0.08f, module.overlayDrawSize);
                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rotation, new Vector3(size, 1f, size));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                if (!AbyssalProtocolMod.Settings.suppressRepeatedWarnings)
                {
                    Log.Warning("[Abyssal Protocol] Modular turret failed to draw animated overlay " + module.defName + ": " + ex.GetType().Name + " " + ex.Message);
                }
            }
        }

        private static Material GetAnimatedOverlayMaterial(string texturePath)
        {
            if (texturePath.NullOrEmpty())
            {
                return null;
            }

            if (AnimatedOverlayMaterialCache.TryGetValue(texturePath, out Material material))
            {
                return material;
            }

            try
            {
                material = MaterialPool.MatFrom(texturePath, ShaderDatabase.MoteGlow);
                AnimatedOverlayMaterialCache[texturePath] = material;
                return material;
            }
            catch
            {
                AnimatedOverlayMaterialCache[texturePath] = null;
                return null;
            }
        }

        private void TickTimedVisualOverlays()
        {
            if (mainDischargeTicksRemaining > 0)
            {
                mainDischargeTicksRemaining--;
                if (mainDischargeTicksRemaining <= 0)
                {
                    StartMainResidualVisual(mainModule);
                }
            }
            else if (mainResidualTicksRemaining > 0)
            {
                mainResidualTicksRemaining--;
            }
        }

        private void StartMainDischargeVisual(ABY_TurretModuleDef module)
        {
            if (module == null || module.dischargeOverlayFramePathPrefix.NullOrEmpty())
            {
                StartMainResidualVisual(module);
                return;
            }

            mainResidualTicksRemaining = 0;
            mainResidualTicksTotal = 0;
            mainDischargeTicksTotal = Mathf.Max(1, Mathf.Max(1, module.dischargeOverlayFrameCount) * Mathf.Max(1, module.dischargeOverlayTicksPerFrame));
            mainDischargeTicksRemaining = mainDischargeTicksTotal;
        }

        private void StartMainResidualVisual(ABY_TurretModuleDef module)
        {
            mainDischargeTicksRemaining = 0;
            mainDischargeTicksTotal = 0;
            if (module == null || module.residualOverlayFramePathPrefix.NullOrEmpty())
            {
                mainResidualTicksRemaining = 0;
                mainResidualTicksTotal = 0;
                return;
            }

            mainResidualTicksTotal = Mathf.Max(1, Mathf.Max(1, module.residualOverlayFrameCount) * Mathf.Max(1, module.residualOverlayTicksPerFrame));
            mainResidualTicksRemaining = mainResidualTicksTotal;
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

        private void StartMainAttack(Thing target)
        {
            if (!IsValidLaunchTarget(target, ResolvedRange, ResolvedMainMinRange))
            {
                return;
            }

            int chargeTicks = ResolvedMainChargeTicks;
            if (chargeTicks > 0)
            {
                StartMainCharge(target, chargeTicks);
                return;
            }

            StartMainBurstNow(target);
        }

        private void StartMainCharge(Thing target, int chargeTicks)
        {
            currentBurstTarget = target;
            mainAimAngle = AngleToTarget(target);
            mainChargeTicksTotal = Mathf.Max(1, chargeTicks);
            mainChargeTicksRemaining = mainChargeTicksTotal;
            if (mainModule?.soundCharge != null)
            {
                ABY_SoundUtility.PlayChargeAt(mainModule.soundCharge.defName, parent.Position, parent.Map);
            }
        }

        private void TickMainCharge()
        {
            if (!Operational || !HasMainWeapon || !IsValidLaunchTarget(currentBurstTarget, ResolvedRange, ResolvedMainMinRange))
            {
                HaltCharge();
                mainCooldownTicks = Mathf.Max(mainCooldownTicks, 45);
                return;
            }

            mainAimAngle = AngleToTarget(currentBurstTarget);
            mainChargeTicksRemaining--;
            if (mainChargeTicksRemaining <= 0)
            {
                Thing target = currentBurstTarget;
                HaltCharge(keepTarget: true);
                StartMainBurstNow(target);
            }
        }

        private void StartMainBurstNow(Thing target)
        {
            if (!IsValidLaunchTarget(target, ResolvedRange, ResolvedMainMinRange))
            {
                HaltCharge();
                return;
            }

            currentBurstTarget = target;
            mainAimAngle = AngleToTarget(target);
            burstShotsRemaining = Mathf.Max(1, mainModule?.burstShotCount ?? 1);
            burstIntervalTicks = 0;
            mainCooldownTicks = ResolvedMainCooldownTicks;
            TickBurst();
        }

        private void TickBurst()
        {
            if (!Operational || !IsValidLaunchTarget(currentBurstTarget, ResolvedRange, ResolvedMainMinRange))
            {
                HaltBurst();
                return;
            }

            mainAimAngle = AngleToTarget(currentBurstTarget);

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

            StartMainDischargeVisual(mainModule);

            burstShotsRemaining--;
            burstIntervalTicks = Mathf.Max(1, mainModule.ticksBetweenBurstShots);

            if (burstShotsRemaining <= 0)
            {
                currentBurstTarget = null;
            }
        }

        private void UpdateVisualAimAngles()
        {
            if (HasMainWeapon)
            {
                Thing target = currentBurstTarget != null && IsValidLaunchTarget(currentBurstTarget, ResolvedRange, ResolvedMainMinRange)
                    ? currentBurstTarget
                    : FindTarget(ResolvedRange, ResolvedMainMinRange);
                if (target != null)
                {
                    mainAimAngle = AngleToTarget(target);
                }
            }

            if (HasAuxiliary)
            {
                float range = ResolvedAuxiliaryRange;
                Thing target = FindTarget(range, ResolvedAuxiliaryMinRange);
                if (target != null)
                {
                    auxiliaryAimAngle = AngleToTarget(target);
                }
            }
        }

        private float AngleToTarget(Thing target)
        {
            if (target == null)
            {
                return 0f;
            }

            Vector3 from = parent.DrawPos;
            Vector3 to = target.DrawPos;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        }

        private Thing FindTarget(float range, float minRange = 0f)
        {
            Map map = parent.Map;
            if (map?.mapPawns == null)
            {
                return null;
            }

            float rangeSquared = range * range;
            float minRangeSquared = Mathf.Max(0f, minRange) * Mathf.Max(0f, minRange);
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
                if (distanceSquared > rangeSquared || distanceSquared < minRangeSquared)
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

        private bool IsValidLaunchTarget(Thing target, float range, float minRange = 0f)
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

            float distanceSquared = target.Position.DistanceToSquared(parent.Position);
            if (range > 0f && distanceSquared > range * range)
            {
                return false;
            }

            if (minRange > 0f && distanceSquared < minRange * minRange)
            {
                return false;
            }

            if (!GenSight.LineOfSight(parent.Position, target.Position, parent.Map, true))
            {
                return false;
            }

            return true;
        }

        private Vector3 ResolveSocketWorldPosition(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return parent.DrawPos;
            }

            Vector3 socketOffset = Vector3.zero;
            if (module.slot == ABY_TurretModuleSlot.Auxiliary)
            {
                socketOffset = new Vector3(Props.auxiliarySocketSideOffset, 0f, Props.auxiliarySocketForwardOffset);
            }
            else if (module.slot == ABY_TurretModuleSlot.MainWeapon)
            {
                socketOffset = new Vector3(Props.mainWeaponSocketSideOffset, 0f, Props.mainWeaponSocketForwardOffset);
            }

            return parent.DrawPos + socketOffset;
        }

        private Vector3 ResolveOverlayPlaneCenter(ABY_TurretModuleDef module, Quaternion rotation)
        {
            Vector3 socketWorldPos = ResolveSocketWorldPosition(module);
            Vector3 centerNudge = new Vector3(module.overlaySideOffset, 0f, module.overlayForwardOffset);
            Vector3 pivotFromTextureCenter = new Vector3(module.overlayPivotSideOffset, 0f, module.overlayPivotForwardOffset);
            return socketWorldPos + rotation * (centerNudge - pivotFromTextureCenter);
        }

        private Vector3 ResolveLaunchOrigin(ABY_TurretModuleDef module, Thing target)
        {
            if (module == null || !module.HasOverlay || module.overlayMuzzleForwardOffset == 0f && module.overlayMuzzleSideOffset == 0f)
            {
                return parent.DrawPos;
            }

            float angle = module.overlayRotatesToTarget && target != null ? AngleToTarget(target) : 0f;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 socketWorldPos = ResolveSocketWorldPosition(module);
            Vector3 muzzleFromSocket = new Vector3(module.overlaySideOffset + module.overlayMuzzleSideOffset, 0f, module.overlayForwardOffset + module.overlayMuzzleForwardOffset);
            Vector3 origin = socketWorldPos + rotation * muzzleFromSocket;
            origin.y = parent.DrawPos.y;
            return origin;
        }

        private float ResolveModuleRange(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return ResolvedRange;
            }

            return module.slot == ABY_TurretModuleSlot.Auxiliary ? ResolvedAuxiliaryRange : ResolvedRange;
        }

        private float ResolveModuleMinRange(ABY_TurretModuleDef module)
        {
            return module != null ? Mathf.Max(0f, module.minRange) : 0f;
        }

        private bool Launch(ABY_TurretModuleDef module, Thing target)
        {
            float range = ResolveModuleRange(module);
            float minRange = ResolveModuleMinRange(module);
            if (module?.projectileDef == null || !IsValidLaunchTarget(target, range, minRange))
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
                Vector3 launchOrigin = ResolveLaunchOrigin(module, target);
                projectile.Launch(parent, launchOrigin, targetInfo, targetInfo, ProjectileHitFlags.IntendedTarget, false, null, null);
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
            mainAimAngle = Mathf.Repeat(mainAimAngle, 360f);
            auxiliaryAimAngle = Mathf.Repeat(auxiliaryAimAngle, 360f);
            burstShotsRemaining = Mathf.Max(0, burstShotsRemaining);
            burstIntervalTicks = Mathf.Max(0, burstIntervalTicks);
            mainChargeTicksRemaining = Mathf.Max(0, mainChargeTicksRemaining);
            mainChargeTicksTotal = Mathf.Max(mainChargeTicksRemaining, mainChargeTicksTotal);
            mainDischargeTicksRemaining = Mathf.Max(0, mainDischargeTicksRemaining);
            mainDischargeTicksTotal = Mathf.Max(mainDischargeTicksRemaining, mainDischargeTicksTotal);
            mainResidualTicksRemaining = Mathf.Max(0, mainResidualTicksRemaining);
            mainResidualTicksTotal = Mathf.Max(mainResidualTicksRemaining, mainResidualTicksTotal);
            if (burstShotsRemaining <= 0 || currentBurstTarget == null || currentBurstTarget.Destroyed)
            {
                HaltBurst();
            }
            if (mainChargeTicksRemaining > 0 && (currentBurstTarget == null || currentBurstTarget.Destroyed))
            {
                HaltCharge();
            }
        }

        private void HaltRuntimeState()
        {
            HaltBurst();
            HaltCharge();
            mainDischargeTicksRemaining = 0;
            mainDischargeTicksTotal = 0;
            mainResidualTicksRemaining = 0;
            mainResidualTicksTotal = 0;
        }

        private void HaltBurst()
        {
            burstShotsRemaining = 0;
            burstIntervalTicks = 0;
            currentBurstTarget = null;
        }

        private void HaltCharge(bool keepTarget = false)
        {
            mainChargeTicksRemaining = 0;
            mainChargeTicksTotal = 0;
            if (!keepTarget)
            {
                currentBurstTarget = null;
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
