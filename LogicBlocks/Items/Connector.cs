using LogicBlocks.Blocks;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace LogicBlocks.Items
{
    internal class Connector : Item
    {
        Pulse? first_block;

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (useType != EnumHandInteract.HeldItemInteract || blockSel == null || Regex.Count(blockSel.Block.Code.ToString(), "^logicblocks:.*") == 0)
                return;
            var system = byEntity.Api.ModLoader.GetModSystem<LogicBlocksModSystem>();
            if (system == null)
            {
                byEntity.Api.Logger.Event("CRITICAL: NO SYSTEM");
                return;
            }

            if (first_block != null)
            {
                byEntity.Api.Logger.Event("SECOND CONNECT AT " + blockSel.Position);
                first_block.connected_blocks.Add(byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as Pulse);
                first_block = null;
            }
            else
            {
                byEntity.Api.Logger.Event("FIRST CONNECT AT " + blockSel.Position);
                first_block = byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as Pulse;
                //first_block = blockSel.Block.GetBlockEntity<Pulse>(blockSel);
            }
        }

    }
}

