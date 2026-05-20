using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Marks abyssal pawn kinds or race defs as valid residue-sintering sources.
    /// Prefer placing this on PawnKindDef so future enemy additions do not require
    /// changes to ABY_ResidueSinteringUtility.
    /// </summary>
    public sealed class ABY_ResidueSinteringExtension : DefModExtension
    {
        /// <summary>
        /// Amount of Abyssal Residue produced from one valid corpse.
        /// Values less than 1 are treated as an explicit block.
        /// </summary>
        public int residueValue = 0;

        /// <summary>
        /// Set false to explicitly block sintering for a pawn kind or race even if
        /// a legacy fallback value would otherwise match the race/kind defName.
        /// </summary>
        public bool allowSintering = true;
    }
}
