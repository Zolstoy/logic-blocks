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
        Logic? first_block;

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is Activator logic_block)
                logic_block.Interact();
            return false;
        }

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
            if (useType == EnumHandInteract.HeldItemInteract)
            {
                if (blockSel == null)
                    return;
                if (this.first_block != null)
                {
                    if (blockSel.Position == this.first_block.Pos)
                    {
                        this.first_block.Unselect();
                        this.first_block = null;
                        return;
                    }

                    if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not Gate gate_block)
                        return;
                    this.first_block.Connect(gate_block);
                    this.first_block.Unselect();
                    this.first_block = null;
                }
                else
                {
                    if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not Logic logic_block)
                        return;
                    this.first_block = logic_block;
                    this.first_block.Select();
                }
            }
        }

        [GeneratedRegex("^logicblocks:.*")]
        private static partial Regex MyRegex();
    }
}

