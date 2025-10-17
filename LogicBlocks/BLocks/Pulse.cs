using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{

    internal class Pulse : Block
    {
        public BlockPos position;
        public List<Block> connected_blocks;

        public Pulse()
        {
            position = new BlockPos(0, 0, 0);
            connected_blocks = [];
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack? byItemStack = null)
        {
            position = blockPos.Copy();
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            foreach (Block block in connected_blocks)
            {

                var pulse = block as Pulse;
                if (pulse != null)
                {
                    return;
                }
                var lineMesh = new MeshData(2, 0, false, false, false, false);

                lineMesh.AddVertex(position.X, position.Y, position.Z, 1, 1);
                lineMesh.AddVertex(pulse.position.X, pulse.position.Y, pulse.position.Z, 1, 1);

                var mesh_ref = capi.Render.UploadMesh(lineMesh);

                capi.Render.RenderMesh(mesh_ref);
                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            }
        }
    }
}
