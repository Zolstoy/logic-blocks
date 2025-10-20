using LogicBlocks.Blocks;
using System.Data;
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
                var pulse = byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as Pulse;
                if (pulse == null)
                {
                    byEntity.Api.Logger.Event("CRITICAL: BLOCK NOT A PULSE AT " + blockSel.Position);
                    return;
                }
                first_block.connected_blocks.Add(pulse);
                first_block = null;
            }
            else
            {
                byEntity.Api.Logger.Event("FIRST CONNECT AT " + blockSel.Position);
                var block = byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (block == null)
                {
                    byEntity.Api.Logger.Event("CRITICAL: BLOCK NOT FOUND AT " + blockSel.Position);
                    return;
                }
                first_block = block as Pulse;
                if (first_block == null)
                {
                    byEntity.Api.Logger.Event("CRITICAL: BLOCK NOT A PULSE AT " + blockSel.Position);
                    return;
                }
            }
        }
    }
}

