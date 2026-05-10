using Verse;

namespace AbyssalProtocol
{
    // Kept intentionally as a no-op compatibility stub for older hover FX packages.
    // The active hover rendering now runs from ABY_HoverArmorDrawPatches directly inside PawnRenderer.RenderPawnAt,
    // so the ring remains locked to the pawn's real cell while the pawn body is visibly offset upward.
    public sealed class ABY_HoverArmorMapComponent : MapComponent
    {
        public ABY_HoverArmorMapComponent(Map map) : base(map)
        {
        }
    }
}
