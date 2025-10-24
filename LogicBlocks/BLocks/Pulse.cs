using System;
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
            api.Logger.Event("PULSE INIT");
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
            if (capi == null || stage != EnumRenderStage.Opaque) return;
            if (meshref == null) return;

            IRenderAPI rpi = capi.Render;
            Vec3d cam = capi.World.Player.Entity.CameraPos;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 1, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
            prog.Tex2D = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

            foreach (BlockEntity block in connected_blocks)
            {
                if (block == null) continue;

                var ray = (block.Pos - Pos).ToVec3d();
                var dist = ray.Length();
                ray = ray.Normalize();
                var angle = Math.Atan2(-ray.Z, ray.X);

                capi.Logger.Event("Angle is " + angle);
                Matrixf modelMat = new Matrixf()
                    .Identity()
                    .Translate(block.Pos.X - cam.X + 0.5, block.Pos.Y - cam.Y + 1, block.Pos.Z - cam.Z + 0.5)
                    .RotateY((float)angle - float.Pi / 2)
                    .Scale(1f, 1f, 10f);

                prog.ModelMatrix = modelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(meshref);
            }

            prog.Stop();
        }


        public void Dispose()
        {
            meshref?.Dispose();
        }
    }
}
