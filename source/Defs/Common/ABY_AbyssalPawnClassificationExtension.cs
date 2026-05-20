using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// XML-owned classification hints for Abyssal pawn kinds and race defs.
    /// Prefer placing this on PawnKindDef so new enemies, bosses, and construct-like pawns
    /// do not need hardcoded C# membership lists in multiple systems.
    /// </summary>
    public sealed class ABY_AbyssalPawnClassificationExtension : DefModExtension
    {
        /// <summary>
        /// Marks this pawn kind/race as part of Abyssal Protocol content.
        /// The default is true because the extension should only be attached to ABY pawn defs.
        /// </summary>
        public bool isAbyssal = true;

        /// <summary>
        /// Major boss or boss-family pawn. Used to protect boss corpses/rewards from generic systems.
        /// </summary>
        public bool isBoss = false;

        /// <summary>
        /// Miniboss or intermediate named encounter pawn. Treated as protected for generic corpse/reward logic.
        /// </summary>
        public bool isMiniBoss = false;

        /// <summary>
        /// Dominion/pocket-map entity or domain infrastructure pawn.
        /// </summary>
        public bool isDominionEntity = false;

        /// <summary>
        /// Mechanical, semi-mechanical, or construct-like physiology. Generic biological bleeding should be suppressed.
        /// </summary>
        public bool constructPhysiology = false;

        /// <summary>
        /// Explicitly block BloodLoss hediffs and active bleeding wounds. Defaults to constructPhysiology when false.
        /// </summary>
        public bool blockBloodLoss = false;
    }
}
