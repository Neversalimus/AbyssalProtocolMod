using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public abstract class Building_ABY_HostileManifestationBase : Building
    {
        protected Faction manifestationFaction;
        protected List<ABY_HostileManifestEntry> manifestationEntries = new List<ABY_HostileManifestEntry>();

        protected int warmupTicks = 90;
        protected int ticksActive;
        protected int seed;
        protected bool completed;

        protected string packLabel;
        protected string letterLabel;
        protected string letterDesc;

        protected float Progress => Mathf.Clamp01(ticksActive / (float)Mathf.Max(1, warmupTicks));

        protected virtual bool CreateAshOnComplete => true;

        public virtual void Initialize(
            Faction faction,
            List<ABY_HostileManifestEntry> entries,
            int warmup,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            manifestationFaction = faction;
            manifestationEntries = new List<ABY_HostileManifestEntry>();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ABY_HostileManifestEntry entry = entries[i];
                    if (entry?.KindDef == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    manifestationEntries.Add(new ABY_HostileManifestEntry(entry.KindDef, entry.Count));
                }
            }

            warmupTicks = Mathf.Max(30, warmup);
            this.packLabel = packLabel;
            this.letterLabel = letterLabel;
            this.letterDesc = letterDesc;
            ticksActive = 0;
            completed = false;
            seed = thingIDNumber > 0 ? thingIDNumber : Rand.Range(1, 1000000);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref manifestationFaction, "manifestationFaction");
            Scribe_Collections.Look(ref manifestationEntries, "manifestationEntries", LookMode.Deep);
            Scribe_Values.Look(ref warmupTicks, "warmupTicks", 90);
            Scribe_Values.Look(ref ticksActive, "ticksActive", 0);
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref completed, "completed", false);
            Scribe_Values.Look(ref packLabel, "packLabel");
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref letterDesc, "letterDesc");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && manifestationEntries == null)
            {
                manifestationEntries = new List<ABY_HostileManifestEntry>();
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksActive++;
            TickManifestation();

            if (!completed && ticksActive >= warmupTicks)
            {
                CompleteManifestation();
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!Spawned || Map == null)
            {
                return;
            }

            DrawManifestation(drawLoc);
        }

        protected virtual void TickManifestation()
        {
        }

        protected virtual void OnManifestationCompleted()
        {
        }

        protected abstract void DrawManifestation(Vector3 drawLoc);

        protected float Pulse(float speed, float offset = 0f)
        {
            return 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * speed + offset);
        }

        protected virtual IntVec3 GetSpawnRootCell()
        {
            return Position;
        }

        private void CompleteManifestation()
        {
            completed = true;

            if (CreateAshOnComplete)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 4);
            }

            OnManifestationCompleted();

            if (manifestationEntries != null && manifestationEntries.Count > 0 && manifestationFaction != null)
            {
                ABY_ArrivalManifestationUtility.TrySpawnManifestedHostiles(
                    Map,
                    GetSpawnRootCell(),
                    manifestationFaction,
                    manifestationEntries,
                    packLabel,
                    letterLabel,
                    letterDesc,
                    sendLetter: false,
                    out _,
                    out _);
            }

            Destroy(DestroyMode.Vanish);
        }

        protected static void DrawPlane(string texPath, Vector3 loc, float scale, float angle, Color color)
        {
            DrawPlane(texPath, loc, scale, scale, angle, color);
        }

        protected static void DrawPlane(string texPath, Vector3 loc, float scaleX, float scaleZ, float angle, Color color)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
