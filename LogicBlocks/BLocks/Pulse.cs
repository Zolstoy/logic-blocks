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
        public Vec3d position;
        public List<Block> connected_blocks;
        public MeshData? mesh_data;
        public MeshRef? mesh;

        public Pulse()
        {


            position = new Vec3d();
            connected_blocks = [];
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack? byItemStack = null)
        {            
            position = blockPos.ToVec3d();
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            foreach (Block block in connected_blocks)
            {
                capi.Logger.Event("FOR CONNECTED BLOCK");

                var pulse = block as Pulse;
                if (pulse == null)
                {
                    capi.Logger.Event("-> CONNECTED BLOCK REMOVED");
                    return;
                }

                if (mesh == null)
                {
                    Shape shape = Vintagestory.API.Common.Shape.TryGet(api, "logicblocks:shapes/block/mymesh.json");
                    var tesselator = ((ICoreClientAPI)api).Tesselator;
                    tesselator.TesselateShape(this, shape, out mesh_data);
                    mesh = capi.Render.UploadMesh(mesh_data);
                }

                capi.Logger.Event("RENDERING: " + position.X + ", " + position.Y + ", " + position.Z);

                capi.Render.GlPushMatrix();
                capi.Render.GlTranslate(position.X + 10, position.Y + 10, position.Z + 10);
                capi.Render.GlScale(3, 3, 3);
                capi.Render.RenderMesh(mesh);
                capi.Render.GlPopMatrix();


                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            }
        }

    }

}
