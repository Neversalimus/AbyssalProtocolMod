using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_AbyssalDashRuntime : MapComponent
    {
        private readonly List<ABY_AbyssalDashInstance> activeDashes = new List<ABY_AbyssalDashInstance>();
        private readonly HashSet<int> activePawnIds = new HashSet<int>();

        public MapComponent_ABY_AbyssalDashRuntime(Map map) : base(map)
        {
        }

        public bool IsPawnDashing(Pawn pawn)
        {
            return pawn != null && activePawnIds.Contains(pawn.thingIDNumber);
        }

        public void StartDash(ABY_AbyssalDashInstance dash)
        {
            if (dash?.Pawn == null || dash.Map != map || IsPawnDashing(dash.Pawn))
            {
                return;
            }

            activeDashes.Add(dash);
            activePawnIds.Add(dash.Pawn.thingIDNumber);
            ABY_AbyssalDashRuntime.SpawnTrailMote(map, dash.SourceCell, dash.TrailMoteDefName, dash.TrailMoteScale);
            ABY_SoundUtility.PlayAt(dash.SoundDefName, dash.SourceCell, map);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (activeDashes.Count == 0)
            {
                return;
            }

            for (int i = activeDashes.Count - 1; i >= 0; i--)
            {
                ABY_AbyssalDashInstance dash = activeDashes[i];
                if (dash == null || dash.Pawn == null || dash.Pawn.Destroyed || dash.Pawn.Dead || !dash.Pawn.Spawned || dash.Pawn.Map != map)
                {
                    RemoveDashAt(i);
                    continue;
                }

                dash.Pawn.pather?.StopDead();
                dash.Pawn.stances?.CancelBusyStanceSoft();

                if (dash.AgeTicks > 0 && dash.AgeTicks % 3 == 0)
                {
                    ABY_AbyssalDashRuntime.SpawnTrailMote(map, dash.Pawn.Position, dash.TrailMoteDefName, dash.TrailMoteScale * 0.72f);
                }

                if (dash.ShouldComplete)
                {
                    RemoveDashAt(i);
                    ABY_AbyssalDashRuntime.TryCompleteDash(dash);
                }
            }
        }

        private void RemoveDashAt(int index)
        {
            ABY_AbyssalDashInstance dash = activeDashes[index];
            if (dash?.Pawn != null)
            {
                activePawnIds.Remove(dash.Pawn.thingIDNumber);
            }
            activeDashes.RemoveAt(index);
        }
    }
}
