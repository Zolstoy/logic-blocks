using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    internal class Pulse : BlockEntity, IRenderer
    {
        private ICoreClientAPI? capi;
        private ICoreServerAPI? sapi;
        private MeshRef? meshref;
        public List<BlockEntity> connected_blocks;
        public List<Vec3i> connected_blocks_coords;
        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public Pulse()
        {
            connected_blocks = [];
            connected_blocks_coords = [];
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("connected_blocks_coords", SerializerUtil.Serialize(connected_blocks_coords));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);

            this.connected_blocks_coords = SerializerUtil.Deserialize<List<Vec3i>>(
                tree.GetBytes("connected_blocks_coords")
            );
            if (this.capi != null && this.capi.Side == EnumAppSide.Client)
            {
                this.capi.Logger.Event("UPDADING CONNECTIONS CLIENT SIDE");
                this.connected_blocks = [];
                foreach (Vec3i block_coords in connected_blocks_coords)
                {
                    var logic_block = capi?.World.BlockAccessor.GetBlockEntity(new BlockPos(block_coords.X, block_coords.Y, block_coords.Z));
                    if (logic_block != null)
                    {
                        this.connected_blocks.Add(logic_block);
                    }
                }
            }
        }

        public void Connect(BlockEntity logic_block)
        {
            this.capi?.Logger.Event("SENDING PACKET 101");
            this.capi?.Network.SendBlockEntityPacket(Pos, 101, SerializerUtil.Serialize(logic_block.Pos.ToVec3i()));
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
            if (packetid == 101)
            {
                var coords = SerializerUtil.Deserialize<Vec3i>(data);
                this.sapi?.Logger.Event("RECEIVED PACKET 101");
                var logic_block = sapi?.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                if (logic_block != null)
                {
                    this.connected_blocks.Add(logic_block);
                    this.connected_blocks_coords.Add(logic_block.Pos.ToVec3i());
                    MarkDirty(true);
                }
                else
                {
                    this.sapi?.Logger.Event($"BLOCK NOT FOUND AT {coords.X},{coords.Y},{coords.Z}");
                }
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            if (api as ICoreServerAPI != null)
            {
                this.sapi = api as ICoreServerAPI;
                this.sapi?.Event.EnqueueMainThreadTask(() =>
                {
                    this.sapi?.Logger.Event($"CONNECTING {connected_blocks_coords.Count} BLOCKS");
                    foreach (var coords in connected_blocks_coords)
                    {
                        var be = this.sapi?.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                        if (be != null)
                        {
                            connected_blocks.Add(be);
                        }
                    }
                }, "restoreconnections");
            }
            if (api as ICoreClientAPI != null)
            {
                this.capi = api as ICoreClientAPI;
                this.capi?.Logger.Event("===> PULSE INIT CLIENT");
                this.capi?.Event.RegisterRenderer(this, EnumRenderStage.Opaque);


                var shape = Shape.TryGet(capi, "logicblocks:shapes/block/mymesh.json");
                if (shape == null)
                {
                    this.capi?.Logger.Event("CRITICAL: COULD NOT LOAD MYMESH FOR CONNECTIONS");
                    return;
                }
                var meshdata = new MeshData();
                this.capi?.Tesselator.TesselateShape(capi.World.BlockAccessor.GetBlock(1), shape, out meshdata);
                if (meshdata != null)
                    this.meshref = this.capi?.Render.UploadMesh(meshdata);
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (this.capi == null || stage != EnumRenderStage.Opaque) return;
            if (this.meshref == null) return;

            IRenderAPI rpi = this.capi.Render;
            Vec3d cam = this.capi.World.Player.Entity.CameraPos;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 1, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
            prog.Tex2D = this.capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

            foreach (BlockEntity block in connected_blocks)
            {
                if (block == null) continue;

                var ray = (block.Pos - Pos).ToVec3d();
                var dist = ray.Length();
                ray = ray.Normalize();
                var angle = Math.Atan2(-ray.Z, ray.X);

                Matrixf modelMat = new Matrixf()
                    .Identity()
                    .Translate(Pos.X - cam.X + 0.5, block.Pos.Y - cam.Y + 1, Pos.Z - cam.Z + 0.5)
                    .RotateY((float)angle + float.Pi / 2)
                    .Scale(1f, 1f, (float)dist * 10f);

                prog.ModelMatrix = modelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(meshref);
            }

            prog.Stop();
        }


        public void Dispose()
        {
            this.meshref?.Dispose();
        }
    }
}
