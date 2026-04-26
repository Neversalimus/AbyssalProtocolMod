using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Package 2 marker for temporary dominion pocket maps.
    /// It is passive on all normal maps and only becomes active when the safe dominion entry marks a map sterile.
    /// </summary>
    public sealed class MapComponent_ABY_SterileAbyssalMap : MapComponent
    {
        private bool sterileDominionPocket;

        public bool IsSterileDominionPocket => sterileDominionPocket;

        public MapComponent_ABY_SterileAbyssalMap(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sterileDominionPocket, "sterileDominionPocket", false);
        }

        public void MarkSterileDominionPocket()
        {
            sterileDominionPocket = true;
        }

        public static bool IsSterile(Map map)
        {
            return map?.GetComponent<MapComponent_ABY_SterileAbyssalMap>()?.IsSterileDominionPocket == true;
        }
    }
}
