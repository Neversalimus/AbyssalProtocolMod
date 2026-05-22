using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Building_ABY_DominionFissure : Building
    {
        private static readonly Dictionary<string, Mesh[]> MeshCache = new Dictionary<string, Mesh[]>();
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static readonly HashSet<int> DrawnThisFrame = new HashSet<int>();
        private static int drawnFrame = -1;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MapComponent_DominionSliceEncounter encounter = map != null ? map.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null)
            {
                encounter.RegisterFissureVisual(this);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map oldMap = Map;
            MapComponent_DominionSliceEncounter encounter = oldMap != null ? oldMap.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null)
            {
                encounter.DeregisterFissureVisual(this);
            }

            base.DeSpawn(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawFissureVisualAt(drawLoc);
        }

        public void DrawFissureVisualFromMapComponent()
        {
            DrawFissureVisualAt(DrawPos);
        }

        private void DrawFissureVisualAt(Vector3 drawLoc)
        {
            if (!TryMarkDrawnThisFrame())
            {
                return;
            }

            DefModExtension_DominionFissure extension = def != null ? def.GetModExtension<DefModExtension_DominionFissure>() : null;
            if (extension == null || extension.sheetTexPath.NullOrEmpty())
            {
                base.DrawAt(drawLoc, false);
                return;
            }

            int frameCount = Mathf.Clamp(extension.frameCount, 1, 64);
            Mesh[] meshes = GetMeshes(extension.sheetTexPath, frameCount);
            if (meshes == null || meshes.Length != frameCount)
            {
                return;
            }

            Material material = GetMaterial(extension);
            if (material == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int frameDuration = Mathf.Max(1, extension.frameDurationTicks);
            int frame = Mathf.Abs(ticks / frameDuration) % frameCount;
            Mesh mesh = meshes[frame];
            if (mesh == null)
            {
                return;
            }

            Vector2 drawSize = extension.DrawSize;
            Vector3 loc = drawLoc;
            loc.x += extension.drawOffsetX;
            loc.z += extension.drawOffsetZ;
            loc.y = AltitudeLayer.FloorEmplacement.AltitudeFor() + extension.altitudeOffset;

            Quaternion rotation = Quaternion.AngleAxis(Rotation.AsAngle, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(loc, rotation, new Vector3(drawSize.x, 1f, drawSize.y));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private bool TryMarkDrawnThisFrame()
        {
            int frame = Time.frameCount;
            if (frame != drawnFrame)
            {
                drawnFrame = frame;
                DrawnThisFrame.Clear();
            }

            return DrawnThisFrame.Add(thingIDNumber);
        }

        private static Material GetMaterial(DefModExtension_DominionFissure extension)
        {
            string key = extension.sheetTexPath + "|" + extension.usePostLightShader + "|" + extension.colorR.ToString("F3") + "|" + extension.colorG.ToString("F3") + "|" + extension.colorB.ToString("F3") + "|" + extension.colorA.ToString("F3");
            Material material;
            if (MaterialCache.TryGetValue(key, out material))
            {
                return material;
            }

            Shader shader = extension.usePostLightShader ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent;
            material = ABY_MaterialCacheUtility.MatFrom(extension.sheetTexPath, shader, extension.DrawColor);
            MaterialCache[key] = material;
            return material;
        }

        private static Mesh[] GetMeshes(string sheetTexPath, int frameCount)
        {
            string key = sheetTexPath + "|" + frameCount;
            Mesh[] meshes;
            if (MeshCache.TryGetValue(key, out meshes))
            {
                return meshes;
            }

            meshes = new Mesh[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                float uMin = i / (float)frameCount;
                float uMax = (i + 1) / (float)frameCount;
                Mesh mesh = new Mesh();
                mesh.name = "ABY_DominionFissureFrame_" + i;
                mesh.vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, -0.5f)
                };
                mesh.uv = new[]
                {
                    new Vector2(uMin, 0f),
                    new Vector2(uMin, 1f),
                    new Vector2(uMax, 1f),
                    new Vector2(uMax, 0f)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateBounds();
                meshes[i] = mesh;
            }

            MeshCache[key] = meshes;
            return meshes;
        }
    }
}
