using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    internal class Pulse : BlockEntity, IRenderer
    {
        private ICoreClientAPI? capi;
        private MeshRef? meshref;
        public List<BlockEntity> connected_blocks;
        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public Pulse()
        {
            connected_blocks = new List<BlockEntity>();
        }

        public override void Initialize(ICoreAPI api)
        {
            this.capi = api as ICoreClientAPI;

            if (this.capi == null)
                return;

            this.capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);


            var shape = Shape.TryGet(capi, "logicblocks:shapes/block/mymesh.json");
            if (shape == null)
            {
                this.capi.Logger.Event("CRITICAL: COULD NOT LOAD MYMESH FOR CONNECTIONS");
                return;
            }
            this.capi.Tesselator.TesselateShape(capi.World.BlockAccessor.GetBlock(1), shape, out var meshdata);
            meshref = this.capi.Render.UploadMesh(meshdata);
        }



        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (capi == null || stage != EnumRenderStage.Opaque)
                return;
            Vec3d cam = capi.World.Player.Entity.CameraPos;

            //capi.Logger.Event("cam=" + cam);
            foreach (BlockEntity block in connected_blocks)
            {
                if (block == null) continue;

                //capi.Render.GlEnableCullFace();
                //capi.Render.GLDisableDepthTest();
                capi.Render.GlPushMatrix();
                capi.Render.GlScale(2.0f, 2.0f, 2.0f);
                capi.Render.GlTranslate(block.Pos.X - cam.X - 0.5, block.Pos.Y - cam.Y, block.Pos.Z - cam.Z - 0.5);
                capi.Render.RenderMesh(meshref);
                capi.Render.GlPopMatrix();
            }
        }

        public void Dispose()
        {
            meshref?.Dispose();
        }
    }
}
