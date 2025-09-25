using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace LogicBlocks.Blocks
{
    internal class BlockGateAnd : Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {

            api.Logger.Event("Block Placed!");
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            api.Logger.Event("Block Broken!");
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}