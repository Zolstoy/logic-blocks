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
        public MeshData? mesh_data;
        public MeshRef? mesh;

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
                capi.Logger.Event("FOR CONNECTED BLOCK");

                var pulse = block as Pulse;
                if (pulse != null)
                {
                    return;
                }

                if (mesh == null)
                {
                    Shape shape = Vintagestory.API.Common.Shape.TryGet(api, "logicblocks:shapes/block/mymesh.json");
                    //IAsset asset = api.Assets.Get(new AssetLocation("logicblocks", "shapes/block/mymesh.json"));
                    //Shape shape = asset.ToObject<Shape>();

                    var tesselator = ((ICoreClientAPI)api).Tesselator;
                    tesselator.TesselateShape(this, shape, out mesh_data);
                    mesh = capi.Render.UploadMesh(mesh_data);
                }

                capi.Logger.Event("RENDERING");

                capi.Render.GlPushMatrix();
                capi.Render.GlTranslate(position.X, position.Y, position.Z);
                capi.Render.RenderMesh(mesh);
                capi.Render.GlPopMatrix();


                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            }
        }
    }
}
