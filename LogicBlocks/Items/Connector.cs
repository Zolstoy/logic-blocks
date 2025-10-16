using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace LogicBlocks.Items
{
    internal class Connector : Item
    {
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (useType == EnumHandInteract.HeldItemInteract)
            {
                byEntity.Api.Logger.Event("CONNECT");
            }
            //base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
        }
    }
}