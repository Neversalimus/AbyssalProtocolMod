using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalBossBarRenderer
    {
        private static readonly Texture2D FrameTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_Frame", false);
        private static readonly Texture2D FillTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_Fill", false);
        private static readonly Texture2D TrailTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_Trail", false);
        private static readonly Texture2D SubFillTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_SubFill", false);
        private static readonly Texture2D IconFrameTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_IconFrame", false);
        private static readonly Texture2D DefaultIconTex = ContentFinder<Texture2D>.Get("UI/AbyssalBossBar/ABY_BossBar_DefaultBossIcon", false);

        private static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>();

        private static int trackedBossId = -1;
        private static float displayedHealthPct = 1f;
        private static float displayedTrailPct = 1f;
        private static float displayedSecondaryPct = 1f;
        private static float displayedAlpha;

        public static void Draw(ABY_BossBarState state)
        {
            if (state?.boss == null || state.profile == null)
            {
                return;
            }

            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            if (!settings.enableBossBars)
            {
                return;
            }

            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || state.boss.MapHeld != Find.CurrentMap)
            {
                return;
            }

            settings.ClampValues();
            ABY_BossBarStylePalette palette = ResolvePalette(state.profile.styleId);
            SyncAnimatedValues(state, settings);

            float scale = settings.globalScale;
            float nameHeight = 22f * scale;
            float barHeight = settings.height * scale;
            float secondaryHeight = state.hasSecondaryBar && settings.showSecondaryBars ? Mathf.Max(10f, barHeight * 0.34f) : 0f;
            float secondaryGap = secondaryHeight > 0f ? 6f * scale : 0f;
            float footerHeight = (settings.showHealthNumbers || settings.showPhaseLabel) ? 18f * scale : 0f;
            float iconSize = settings.iconSize * scale;
            float gap = settings.gap * scale;
            float barWidth = settings.width * scale;
            float totalWidth = iconSize + gap + barWidth;
            float totalHeight = Mathf.Max(iconSize, nameHeight + barHeight + secondaryGap + secondaryHeight + footerHeight);

            Rect screenRect = UI.screenWidth > 0 && UI.screenHeight > 0
                ? new Rect(0f, 0f, UI.screenWidth, UI.screenHeight)
                : new Rect(0f, 0f, Screen.width, Screen.height);
            Vector2 topLeft = settings.ResolveTopLeft(screenRect, new Vector2(totalWidth, totalHeight));
            Rect rootRect = new Rect(topLeft.x, topLeft.y, totalWidth, totalHeight);
            Rect iconRect = new Rect(rootRect.x, rootRect.y + Mathf.Max(0f, totalHeight - iconSize) * 0.5f, iconSize, iconSize);
            Rect textRoot = new Rect(iconRect.xMax + gap, rootRect.y, barWidth, totalHeight);
            Rect nameRect = new Rect(textRoot.x, textRoot.y, textRoot.width, nameHeight);
            Rect mainBarRect = new Rect(textRoot.x, nameRect.yMax + 4f * scale, textRoot.width, barHeight);
            Rect secondaryRect = new Rect(mainBarRect.x, mainBarRect.yMax + secondaryGap, mainBarRect.width, secondaryHeight);
            Rect footerRect = new Rect(mainBarRect.x, totalHeight - footerHeight > 0f ? rootRect.yMax - footerHeight : secondaryRect.yMax, mainBarRect.width, footerHeight);

            DrawBackdrop(rootRect, palette, displayedAlpha, settings.reducedMotion);
            DrawIcon(iconRect, state, palette, displayedAlpha);
            DrawName(nameRect, state.displayLabel, palette, displayedAlpha);
            DrawMainBar(mainBarRect, state, palette, displayedAlpha, settings);

            if (secondaryHeight > 0f)
            {
                DrawSecondaryBar(secondaryRect, state, palette, displayedAlpha, settings);
            }

            if (footerHeight > 0f)
            {
                DrawFooter(footerRect, state, palette, displayedAlpha, settings);
            }

            if (settings.showCalibrationButton)
            {
                DrawCalibrationButton(new Rect(rootRect.xMax - 84f * scale, rootRect.y - 26f * scale, 84f * scale, 22f * scale));
            }
        }

        private static void SyncAnimatedValues(ABY_BossBarState state, AbyssalProtocolModSettings settings)
        {
            int bossId = state.boss.thingIDNumber;
            if (bossId != trackedBossId)
            {
                trackedBossId = bossId;
                displayedHealthPct = state.healthPct;
                displayedTrailPct = state.healthPct;
                displayedSecondaryPct = state.secondaryPct;
                displayedAlpha = 0f;
            }

            float realtime = Time.unscaledDeltaTime <= 0f ? 0.016f : Time.unscaledDeltaTime;
            float fastLerp = settings.reducedMotion ? 16f : 11.5f;
            float trailLerp = settings.reducedMotion ? 10f : 3.25f;

            displayedAlpha = Mathf.MoveTowards(displayedAlpha, 1f, realtime * (settings.reducedMotion ? 6f : 3.2f));
            displayedHealthPct = Mathf.Lerp(displayedHealthPct, state.healthPct, 1f - Mathf.Exp(-fastLerp * realtime));

            if (state.healthPct >= displayedTrailPct)
            {
                displayedTrailPct = Mathf.Lerp(displayedTrailPct, state.healthPct, 1f - Mathf.Exp(-(fastLerp + 2f) * realtime));
            }
            else
            {
                displayedTrailPct = Mathf.Lerp(displayedTrailPct, state.healthPct, 1f - Mathf.Exp(-trailLerp * realtime));
            }

            if (state.hasSecondaryBar)
            {
                displayedSecondaryPct = Mathf.Lerp(displayedSecondaryPct, state.secondaryPct, 1f - Mathf.Exp(-fastLerp * realtime));
            }
            else
            {
                displayedSecondaryPct = 0f;
            }

            displayedHealthPct = Mathf.Clamp01(displayedHealthPct);
            displayedTrailPct = Mathf.Clamp01(displayedTrailPct);
            displayedSecondaryPct = Mathf.Clamp01(displayedSecondaryPct);
        }

        private static void DrawBackdrop(Rect rect, ABY_BossBarStylePalette palette, float alpha, bool reducedMotion)
        {
            float pulse = reducedMotion ? 0.15f : 0.20f + Mathf.Sin(Time.realtimeSinceStartup * 2.3f) * 0.08f;
            Color oldColor = GUI.color;
            GUI.color = new Color(palette.backdrop.r, palette.backdrop.g, palette.backdrop.b, alpha * 0.86f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = new Color(palette.glow.r, palette.glow.g, palette.glow.b, alpha * (0.09f + pulse));
            GUI.DrawTexture(rect.ContractedBy(2f), BaseContent.WhiteTex);
            GUI.color = oldColor;
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), new Color(palette.border.r, palette.border.g, palette.border.b, alpha * 0.9f));
        }

        private static void DrawIcon(Rect rect, ABY_BossBarState state, ABY_BossBarStylePalette palette, float alpha)
        {
            Texture2D icon = ResolveIconTexture(state.profile);
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, IconFrameTex ?? BaseContent.WhiteTex, ScaleMode.StretchToFill, true);
            GUI.color = new Color(palette.iconTint.r, palette.iconTint.g, palette.iconTint.b, alpha);
            GUI.DrawTexture(rect.ContractedBy(rect.width * 0.12f), icon ?? DefaultIconTex ?? BaseContent.BadTex, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        private static void DrawName(Rect rect, string label, ABY_BossBarStylePalette palette, float alpha)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Color oldColor = GUI.color;
            GUI.color = new Color(palette.text.r, palette.text.g, palette.text.b, alpha);
            Widgets.Label(rect, label);
            GUI.color = oldColor;
        }

        private static void DrawMainBar(Rect rect, ABY_BossBarState state, ABY_BossBarStylePalette palette, float alpha, AbyssalProtocolModSettings settings)
        {
            Rect innerRect = rect.ContractedBy(2f);
            Widgets.DrawBoxSolid(rect, new Color(0.04f, 0.04f, 0.05f, alpha * 0.94f));
            Widgets.DrawBoxSolid(innerRect, new Color(palette.backFill.r, palette.backFill.g, palette.backFill.b, alpha * 0.96f));

            if (displayedTrailPct > 0.001f)
            {
                Rect trailRect = new Rect(innerRect.x, innerRect.y, innerRect.width * displayedTrailPct, innerRect.height);
                DrawTexturedFill(trailRect, TrailTex, new Color(palette.trail.r, palette.trail.g, palette.trail.b, alpha * 0.72f));
            }

            if (displayedHealthPct > 0.001f)
            {
                Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * displayedHealthPct, innerRect.height);
                DrawTexturedFill(fillRect, FillTex, new Color(palette.fill.r, palette.fill.g, palette.fill.b, alpha));

                if (!settings.reducedMotion && fillRect.width > 24f)
                {
                    float sheenWidth = Mathf.Min(96f, fillRect.width * 0.28f);
                    float sheenX = fillRect.x - sheenWidth + Mathf.Repeat(Time.realtimeSinceStartup * 110f, fillRect.width + sheenWidth);
                    Widgets.DrawBoxSolid(new Rect(sheenX, fillRect.y, sheenWidth, fillRect.height), new Color(1f, 0.95f, 0.85f, alpha * 0.14f));
                }
            }

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, FrameTex ?? BaseContent.WhiteTex, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;

            if (state.profile.showPhaseMarkers && settings.showPhaseMarkers)
            {
                DrawPhaseMarkers(rect, innerRect, state, palette, alpha);
            }
        }

        private static void DrawSecondaryBar(Rect rect, ABY_BossBarState state, ABY_BossBarStylePalette palette, float alpha, AbyssalProtocolModSettings settings)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.035f, 0.04f, 0.05f, alpha * 0.92f));
            Rect innerRect = rect.ContractedBy(2f);
            Widgets.DrawBoxSolid(innerRect, new Color(0.07f, 0.10f, 0.14f, alpha * 0.94f));
            if (displayedSecondaryPct > 0.001f)
            {
                Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * displayedSecondaryPct, innerRect.height);
                DrawTexturedFill(fillRect, SubFillTex, new Color(palette.secondaryFill.r, palette.secondaryFill.g, palette.secondaryFill.b, alpha));
            }

            Widgets.DrawBox(rect, 1);
            if (state.secondaryLabel.NullOrEmpty())
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Color oldColor = GUI.color;
            GUI.color = new Color(palette.secondaryText.r, palette.secondaryText.g, palette.secondaryText.b, alpha);
            Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), state.secondaryLabel);
            if (settings.showHealthNumbers)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), Mathf.RoundToInt(state.secondaryCurrent) + " / " + Mathf.RoundToInt(state.secondaryMax));
            }

            GUI.color = oldColor;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawFooter(Rect rect, ABY_BossBarState state, ABY_BossBarStylePalette palette, float alpha, AbyssalProtocolModSettings settings)
        {
            Text.Font = GameFont.Tiny;
            Color oldColor = GUI.color;

            if (settings.showPhaseLabel)
            {
                GUI.color = new Color(palette.phaseText.r, palette.phaseText.g, palette.phaseText.b, alpha);
                Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.45f, rect.height), "ABY_BossBar_PhaseLabel".Translate(state.currentPhaseLabel));
            }

            if (settings.showHealthNumbers)
            {
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = new Color(palette.text.r, palette.text.g, palette.text.b, alpha);
                Widgets.Label(rect, Mathf.RoundToInt(state.currentHealth) + " / " + Mathf.RoundToInt(state.maxHealth));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            GUI.color = oldColor;
        }

        private static void DrawCalibrationButton(Rect rect)
        {
            if (rect.width < 20f || rect.height < 12f)
            {
                return;
            }

            if (AbyssalStyledWidgets.TextButton(rect, "ABY_BossBar_AdjustShort".Translate()))
            {
                Window_ABY_BossBarCalibration.OpenWindow();
            }
        }

        private static void DrawPhaseMarkers(Rect frameRect, Rect innerRect, ABY_BossBarState state, ABY_BossBarStylePalette palette, float alpha)
        {
            List<ABY_BossBarPhaseSnapshot> phases = state.phases;
            if (phases == null || phases.Count == 0)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            for (int i = 0; i < phases.Count; i++)
            {
                ABY_BossBarPhaseSnapshot phase = phases[i];
                if (phase == null || phase.phaseIndex <= 1)
                {
                    continue;
                }

                float pct = Mathf.Clamp01(phase.triggerHealthPct);
                float x = innerRect.x + innerRect.width * pct;
                Color tickColor = phase.current ? palette.border : (phase.reached ? palette.phaseReached : palette.phasePending);
                Widgets.DrawBoxSolid(new Rect(x - 1f, frameRect.y + 2f, 2f, frameRect.height - 4f), new Color(tickColor.r, tickColor.g, tickColor.b, alpha * 0.92f));
                if (!phase.label.NullOrEmpty())
                {
                    Color oldColor = GUI.color;
                    GUI.color = new Color(tickColor.r, tickColor.g, tickColor.b, alpha);
                    Rect labelRect = new Rect(x - 16f, frameRect.y - 14f, 32f, 14f);
                    Text.Anchor = TextAnchor.UpperCenter;
                    Widgets.Label(labelRect, phase.label);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = oldColor;
                }
            }
        }

        private static void DrawTexturedFill(Rect rect, Texture2D texture, Color color)
        {
            if (rect.width <= 0.5f || rect.height <= 0.5f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture ?? BaseContent.WhiteTex, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private static Texture2D ResolveIconTexture(ABY_BossBarProfileDef profile)
        {
            if (profile == null || profile.iconTexPath.NullOrEmpty())
            {
                return DefaultIconTex;
            }

            if (IconCache.TryGetValue(profile.iconTexPath, out Texture2D cached))
            {
                return cached ?? DefaultIconTex;
            }

            Texture2D loaded = ContentFinder<Texture2D>.Get(profile.iconTexPath, false);
            IconCache[profile.iconTexPath] = loaded;
            return loaded ?? DefaultIconTex;
        }

        private static ABY_BossBarStylePalette ResolvePalette(string styleId)
        {
            switch (styleId)
            {
                case "abyssal_rupture":
                    return new ABY_BossBarStylePalette(
                        new Color(0.12f, 0.05f, 0.055f, 0.96f),
                        new Color(0.18f, 0.08f, 0.08f, 1f),
                        new Color(0.70f, 0.09f, 0.10f, 1f),
                        new Color(1f, 0.20f, 0.20f, 1f),
                        new Color(1f, 0.68f, 0.60f, 1f),
                        new Color(1f, 0.90f, 0.90f, 1f),
                        new Color(1f, 0.74f, 0.66f, 1f),
                        new Color(0.72f, 0.25f, 0.28f, 1f),
                        new Color(0.34f, 0.14f, 0.15f, 1f),
                        new Color(0.16f, 0.10f, 0.11f, 1f),
                        new Color(0.84f, 0.14f, 0.14f, 1f),
                        new Color(1f, 0.30f, 0.26f, 0.60f));
                case "abyssal_reactor_saint":
                    return new ABY_BossBarStylePalette(
                        new Color(0.07f, 0.08f, 0.11f, 0.96f),
                        new Color(0.10f, 0.10f, 0.14f, 1f),
                        new Color(0.86f, 0.42f, 0.18f, 1f),
                        new Color(1f, 0.58f, 0.24f, 1f),
                        new Color(0.62f, 0.92f, 1f, 1f),
                        new Color(0.94f, 0.97f, 1f, 1f),
                        new Color(0.74f, 0.90f, 1f, 1f),
                        new Color(0.72f, 0.82f, 0.90f, 1f),
                        new Color(0.30f, 0.42f, 0.52f, 1f),
                        new Color(0.14f, 0.18f, 0.22f, 1f),
                        new Color(0.62f, 0.92f, 1f, 1f),
                        new Color(0.30f, 0.70f, 0.95f, 0.55f));
                default:
                    return new ABY_BossBarStylePalette(
                        new Color(0.11f, 0.07f, 0.06f, 0.96f),
                        new Color(0.15f, 0.09f, 0.07f, 1f),
                        new Color(0.86f, 0.30f, 0.12f, 1f),
                        new Color(1f, 0.54f, 0.22f, 1f),
                        new Color(1f, 0.78f, 0.62f, 1f),
                        new Color(0.98f, 0.93f, 0.88f, 1f),
                        new Color(1f, 0.82f, 0.70f, 1f),
                        new Color(0.74f, 0.40f, 0.24f, 1f),
                        new Color(0.32f, 0.18f, 0.12f, 1f),
                        new Color(0.15f, 0.10f, 0.08f, 1f),
                        new Color(0.96f, 0.44f, 0.18f, 1f),
                        new Color(1f, 0.58f, 0.24f, 0.50f));
            }
        }

        private sealed class ABY_BossBarStylePalette
        {
            public readonly Color backdrop;
            public readonly Color backFill;
            public readonly Color trail;
            public readonly Color fill;
            public readonly Color border;
            public readonly Color text;
            public readonly Color iconTint;
            public readonly Color phaseReached;
            public readonly Color phasePending;
            public readonly Color phaseText;
            public readonly Color secondaryFill;
            public readonly Color secondaryText;
            public readonly Color glow;

            public ABY_BossBarStylePalette(
                Color backdrop,
                Color backFill,
                Color trail,
                Color fill,
                Color border,
                Color text,
                Color iconTint,
                Color phaseReached,
                Color phasePending,
                Color phaseText,
                Color secondaryFill,
                Color secondaryText)
            {
                this.backdrop = backdrop;
                this.backFill = backFill;
                this.trail = trail;
                this.fill = fill;
                this.border = border;
                this.text = text;
                this.iconTint = iconTint;
                this.phaseReached = phaseReached;
                this.phasePending = phasePending;
                this.phaseText = phaseText;
                this.secondaryFill = secondaryFill;
                this.secondaryText = secondaryText;
                glow = new Color(fill.r, fill.g, fill.b, 0.15f);
            }
        }
    }
}
