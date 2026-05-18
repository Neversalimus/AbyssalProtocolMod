using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HoverArmorStatPartInjector
    {
        static ABY_HoverArmorStatPartInjector()
        {
            try
            {
                StatDef moveSpeed = StatDefOf.MoveSpeed;
                if (moveSpeed == null)
                {
                    return;
                }

                if (moveSpeed.parts == null)
                {
                    moveSpeed.parts = new List<StatPart>();
                }

                for (int i = 0; i < moveSpeed.parts.Count; i++)
                {
                    if (moveSpeed.parts[i] is StatPart_ABY_DraftedHoverArmorSpeed)
                    {
                        return;
                    }
                }

                moveSpeed.parts.Add(new StatPart_ABY_DraftedHoverArmorSpeed());
            }
            catch (System.Exception ex)
            {
                ABY_LogThrottleUtility.Warning("hoverArmorStatPartInject", "[Abyssal Protocol] Failed to inject drafted hover armor speed stat part: " + ex.Message, 600);
            }
        }
    }
}
