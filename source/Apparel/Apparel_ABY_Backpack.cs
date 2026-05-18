using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace AbyssalProtocol
{
    public class Apparel_ABY_Backpack : Apparel
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        public override void DrawWornExtras()
        {
            base.DrawWornExtras();

            Pawn wearer = Wearer;
            if (!ShouldDrawBackpack(wearer))
            {
                return;
            }

            string texPath = ResolveDirectionalTexPath(def.defName, wearer.Rotation);
            Material material = GetMaterial(texPath);
            if (material == null)
            {
                return;
            }

            BackpackDrawProfile profile = ResolveDrawProfile(def.defName, wearer.Rotation);
            Vector3 drawPos = wearer.DrawPos + profile.Offset;
            Quaternion rotation = Quaternion.AngleAxis(profile.Angle, Vector3.up);
            Vector3 scale = new Vector3(profile.Size.x, 1f, profile.Size.y);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, rotation, scale), material, 0);
        }

        private static bool ShouldDrawBackpack(Pawn wearer)
        {
            if (wearer == null || wearer.Dead || wearer.Rotation == Rot4.Invalid)
            {
                return false;
            }

            if (wearer.Drawer == null || wearer.MapHeld == null)
            {
                return false;
            }

            return true;
        }

        private static Material GetMaterial(string texPath)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return null;
            }

            if (MaterialCache.TryGetValue(texPath, out Material material) && material != null)
            {
                return material;
            }

            try
            {
                material = GraphicDatabase.Get<Graphic_Single>(texPath, ShaderDatabase.Cutout, Vector2.one, Color.white).MatSingle;
                MaterialCache[texPath] = material;
                return material;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Failed to resolve backpack texture '" + texPath + "': " + ex.Message);
                MaterialCache[texPath] = null;
                return null;
            }
        }

        private static string ResolveDirectionalTexPath(string defName, Rot4 rot)
        {
            string basePath = "Things/Apparel/" + defName;
            if (rot == Rot4.North)
            {
                return basePath + "_north";
            }

            if (rot == Rot4.East || rot == Rot4.West)
            {
                return basePath + "_east";
            }

            return basePath + "_south";
        }

        private static BackpackDrawProfile ResolveDrawProfile(string defName, Rot4 rot)
        {
            float tierScale = 1f;
            if (string.Equals(defName, "ABY_RiftRelayPack", StringComparison.Ordinal))
            {
                tierScale = 1.06f;
            }
            else if (string.Equals(defName, "ABY_CrownConduitPack", StringComparison.Ordinal))
            {
                tierScale = 1.12f;
            }

            if (rot == Rot4.North)
            {
                return new BackpackDrawProfile(
                    new Vector2(1.04f * tierScale, 1.04f * tierScale),
                    new Vector3(0f, 0.035f, 0.035f),
                    0f);
            }

            if (rot == Rot4.East)
            {
                return new BackpackDrawProfile(
                    new Vector2(0.82f * tierScale, 0.98f * tierScale),
                    new Vector3(-0.18f, 0.032f, 0.035f),
                    0f);
            }

            if (rot == Rot4.West)
            {
                return new BackpackDrawProfile(
                    new Vector2(0.82f * tierScale, 0.98f * tierScale),
                    new Vector3(0.18f, 0.032f, 0.035f),
                    180f);
            }

            return new BackpackDrawProfile(
                new Vector2(0.92f * tierScale, 0.92f * tierScale),
                new Vector3(0f, 0.028f, 0.02f),
                0f);
        }

        private readonly struct BackpackDrawProfile
        {
            public readonly Vector2 Size;
            public readonly Vector3 Offset;
            public readonly float Angle;

            public BackpackDrawProfile(Vector2 size, Vector3 offset, float angle)
            {
                Size = size;
                Offset = offset;
                Angle = angle;
            }
        }
    }
}
