using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace LogicBlocks.Items
{
    internal class Connector : Item
    {
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (useType == EnumHandInteract.HeldItemInteract && blockSel != null)
            {
                if (Regex.Count(blockSel.Block.Code.ToString(), "^logicblocks:.*") > 0) {

                    var system = byEntity.Api.ModLoader.GetModSystem<LogicBlocksModSystem>();
                    if (system == null)
                    {
                        byEntity.Api.Logger.Event("CRITICAL: NO SYSTEM");
                        return ;
                    }


                }
            }

            //base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
        }
    }
}
