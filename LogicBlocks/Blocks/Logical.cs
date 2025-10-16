using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    public abstract class Logical : BlockEntity, IRenderer
    {
        private LogicBlocksModSystem? system;
        protected ICoreClientAPI? capi;
        private ICoreServerAPI? sapi;
        private MeshRef? meshref;
        private List<Logical>? connected_blocks;
        private List<Vec3i>? connected_coords;
        public bool state;
        private double timer;
        private long listener_id;
        protected List<Logical>? parent_blocks;

        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public void Remove(BlockPos pos)
        {
            this.connected_blocks?.RemoveAll(b => b.Pos == pos);
            this.connected_coords?.RemoveAll(b => b == pos.ToVec3i());
        }

        private void Tick(float dt)
        {
            this.timer += dt;

            for (int i = 0; i < connected_blocks.Count; i++)
                this.connected_blocks[i].Trigger();

            if (timer >= 2)
            {
                foreach (IServerPlayer player in this.sapi.Server.Players)
                    this.sapi?.Network.SendBlockEntityPacket(player as IServerPlayer, Pos, 120, SerializerUtil.Serialize(false));
                this.state = false;
                this.timer = 0;
                this.UnregisterGameTickListener(this.listener_id);
            }
        }

        protected abstract bool CanTrigger();

        public void Trigger()
        {
            if (!this.state)
            {
                if (!this.CanTrigger())
                    return ;
                foreach (IServerPlayer player in this.sapi.Server.Players)
                    this.sapi?.Network.SendBlockEntityPacket(player as IServerPlayer, Pos, 120, SerializerUtil.Serialize(true));
                this.state = true;
                this.listener_id = this.RegisterGameTickListener(dt => this.Tick(dt), 50);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("connected_blocks_coords", SerializerUtil.Serialize(this.connected_coords));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.connected_coords = SerializerUtil.Deserialize<List<Vec3i>>(
                tree.GetBytes("connected_blocks_coords")
            );

        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 102)
                this.connected_coords = SerializerUtil.Deserialize<List<Vec3i>>(data);
            else if (packetid == 98)
            {
                var pos = SerializerUtil.Deserialize<Vec3i>(data);
                this.connected_coords?.Remove(pos);
            }
            else if (packetid == 120)
                this.state = SerializerUtil.Deserialize<bool>(data);
        }

        public override void OnBlockBroken(IPlayer byPlayer)
        {
            base.OnBlockBroken(byPlayer);
            this.capi?.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            this.capi?.Network.SendBlockEntityPacket(Pos, 99, SerializerUtil.Serialize(Pos));
        }

        public void Connect(Logical logic_block)
        {
            if (logic_block.Pos == Pos)
                return;
            this.capi?.Network.SendBlockEntityPacket(Pos, 101, SerializerUtil.Serialize(logic_block.Pos.ToVec3i()));
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
            if (packetid == 101)
            {
                var coords = SerializerUtil.Deserialize<Vec3i>(data);
                var block = sapi?.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                if (block != null)
                {
                    if (block is Logical logic_block)
                    {
                        foreach (var connected_to_connected in logic_block.connected_blocks)
                            if (connected_to_connected.Pos == Pos)
                                return;
                        this.connected_blocks?.Add(logic_block);
                        logic_block.parent_blocks.Add(this);
                        this.connected_coords?.Add(logic_block.Pos.ToVec3i());
                        this.Sync();
                    }
                }
            }
            else if (packetid == 100)
            {
                this.Sync();
            }
            else if (packetid == 99)
            {
                this.connected_blocks = [];
                this.system?.BroadcastRemove(this.Pos);
            }
            else if (packetid == 110)
            {
                Trigger();
            }
        }

        private void Sync()
        {
            var to_send = SerializerUtil.Serialize(this.connected_coords);
            if (this.sapi == null)
                return;
            foreach (IServerPlayer player in this.sapi.Server.Players)
                this.sapi?.Network.SendBlockEntityPacket(player as IServerPlayer, Pos, 102, to_send);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api as ICoreServerAPI != null)
            {
                this.connected_blocks = [];
                this.connected_coords ??= [];
                this.parent_blocks = [];
                this.sapi = api as ICoreServerAPI;
                foreach (var coords in this.connected_coords)
                {
                    var block = this.sapi?.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                    if (block is Logical logic_block)
                    {
                        this.connected_blocks?.Add(logic_block);
                        logic_block.parent_blocks.Add(this);
                    }
                }
                this.sapi.Event.EnqueueMainThreadTask(() => {
                    foreach (var block in this.connected_blocks)
                    {
                        block.parent_blocks.Add(this);
                    }
                }, $"logicblocks:restoreparents");
                this.system = this.sapi?.ModLoader.GetModSystem<LogicBlocksModSystem>();
                this.system?.RegisterLogicBlock(this);
            }
            if (api as ICoreClientAPI != null)
            {
                this.connected_coords = [];
                this.capi = api as ICoreClientAPI;
                this.capi?.Network.SendBlockEntityPacket(Pos, 100, SerializerUtil.Serialize(""));
                this.capi?.Event.RegisterRenderer(this, EnumRenderStage.Opaque);

                var shape = Shape.TryGet(capi, "logicblocks:shapes/connection.json");
                if (shape == null)
                    return;
                var meshdata = new MeshData();
                this.capi?.Tesselator.TesselateShape(capi.World.BlockAccessor.GetBlock(1), shape, out meshdata);
                if (meshdata != null)
                    this.meshref = this.capi?.Render.UploadMesh(meshdata);
            }
        }

        private void RenderConnection(IRenderAPI rpi, Vec3d cam)
        {
            if (this.capi == null || this.meshref == null || this.connected_coords == null)
                return;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 0, 0, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
            prog.Tex2D = this.capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

            foreach (Vec3i block in this.connected_coords)
            {
                var rayi = (block - Pos.ToVec3i());
                var ray = new Vec3d(rayi.X, rayi.Y, rayi.Z);
                var dist = ray.Length();
                ray = ray.Normalize();
                var angleZ = Math.Atan2(ray.Y, Math.Sqrt(ray.X * ray.X + ray.Z * ray.Z));
                var angleY = Math.Atan2(-ray.Z, ray.X);
                var translation = Pos.ToVec3d() + new Vec3d(0.5, 0.5, 0.5) - cam + ray * dist / 2;
                Matrixf modelMat = new Matrixf()
                    .Identity()
                    .Translate(translation.X, translation.Y, translation.Z)
                    .RotateY((float)angleY + float.Pi / 2)
                    .RotateX((float)-angleZ)
                    .Scale(1f, 1f, (float)dist * 12f);

                prog.ModelMatrix = modelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(meshref);
            }

            prog.Stop();
        }

        public void OnRenderFrame(float _delta, EnumRenderStage stage)
        {
            if (this.capi == null || stage != EnumRenderStage.Opaque)
                return;

            IRenderAPI rpi = this.capi.Render;
            Vec3d cam = this.capi.World.Player.Entity.CameraPos;

            if (this.connected_coords?.Count > 0)
                this.RenderConnection(rpi, cam);

            if (this.state)
                this.RenderTriggered(rpi, cam);

        }

        private void RenderTriggered(IRenderAPI rpi, Vec3d cam)
        {
            if (this.capi == null || this.meshref == null)
                return;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(0, 0, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 1, 0);
            prog.Tex2D = this.capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

            Matrixf modelMat = new Matrixf()
                .Identity()
                .Translate(Pos.X + 0.5 - cam.X, Pos.Y + 0.5 - cam.Y, Pos.Z + 0.5 - cam.Z)
                .Scale(20f, 20f, 20f);

            prog.ModelMatrix = modelMat.Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(meshref);

            prog.Stop();
        }

        public void Dispose()
        {
            this.meshref?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
