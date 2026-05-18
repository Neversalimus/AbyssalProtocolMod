using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class StatPart_ABY_DraftedHoverArmorSpeed : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            float bonus = ABY_HoverArmorUtility.GetDraftedMoveSpeedBonus(pawn);
            if (bonus > 0f)
            {
                val += bonus;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            float bonus = ABY_HoverArmorUtility.GetDraftedMoveSpeedBonus(pawn);
            if (bonus <= 0f)
            {
                return null;
            }

            return "Abyssal drafted flight rig: +" + bonus.ToString("0.##") + " c/s";
        }
    }
}
