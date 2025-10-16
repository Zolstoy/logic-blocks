using LogicBlocks.Blocks;
using System.Data;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace LogicBlocks.Items
{
    internal partial class Connector : Item
    {
        Logical? first_block;

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is Activable logic_block)
                logic_block.Activate();
            return false;
        }

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
            if (useType == EnumHandInteract.HeldItemInteract)
            {
                if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not Logical logic_block)
                    return;
                if (this.first_block != null)
                {
                    if (blockSel.Position != this.first_block.Pos)    
                        this.first_block.Connect(logic_block);
                    this.first_block = null;
                }
                else
                    this.first_block = logic_block;
            }
        }

        [GeneratedRegex("^logicblocks:.*")]
        private static partial Regex MyRegex();
    }
}

