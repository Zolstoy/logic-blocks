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
        Pulse? first_block;

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
            EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (useType != EnumHandInteract.HeldItemInteract || blockSel == null || MyRegex().Count(blockSel.Block.Code.ToString()) == 0)
                return;
            var system = byEntity.Api.ModLoader.GetModSystem<LogicBlocksModSystem>();
            if (system == null)
            {
                byEntity.Api.Logger.Event("CRITICAL: NO SYSTEM");
                return;
            }

            if (this.first_block != null)
            {
                byEntity.Api.Logger.Event("SECOND CONNECT AT " + blockSel.Position);
                if (byEntity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not Pulse pulse)
                {
                    byEntity.Api.Logger.Event("CRITICAL: BLOCK NOT A PULSE AT " + blockSel.Position);
                    return;
                }
                this.first_block.Connect(pulse);
                this.first_block = null;
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
                this.first_block = block as Pulse;
                if (this.first_block == null)
                {
                    byEntity.Api.Logger.Event("CRITICAL: BLOCK NOT A PULSE AT " + blockSel.Position);
                    return;
                }
            }
        }

        [GeneratedRegex("^logicblocks:.*")]
        private static partial Regex MyRegex();
    }
}

