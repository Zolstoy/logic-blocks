using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using static System.Net.Mime.MediaTypeNames;

namespace LogicBlocks.Blocks
{
    public abstract class Logic : BlockEntity, IRenderer
    {
        public enum ClientAction
        {
            Connect = 101,
            Sync = 102,
            Destroy = 103,
            Trigger = 104,
            GetState = 105,
        }

        public enum ServerState
        {
            Sync = 201,
            Remove = 202,
            ChangeState = 203
        }

        public class ServerResources(ICoreServerAPI api, LogicBlocksModSystem system, List<Gate> connected_blocks, List<Logic> parent_blocks)
        {
            public ICoreServerAPI api = api;
            public LogicBlocksModSystem system = system;
            public List<Gate> connected_blocks = connected_blocks;
            public List<Logic> parent_blocks = parent_blocks;
        }

        protected class ClientResources(ICoreClientAPI api, MeshRef connection_false_meshref, MeshRef connection_true_meshref, MeshRef triggered_meshref, MeshRef selected_meshref, List<Vec3i> connected_coords)
        {
            public ICoreClientAPI api = api;
            public MeshRef connection_false_meshref = connection_false_meshref;
            public MeshRef connection_true_meshref = connection_true_meshref;
            public MeshRef triggered_meshref = triggered_meshref;
            public MeshRef selected_meshref = selected_meshref;
            public List<Vec3i> connected_coords = connected_coords;
            internal bool selected = false;
            internal float render_timer;
        }

        public List<Vec3i> connected_coords = [];
        public bool state = false;
        protected ServerResources? server;
        protected ClientResources? client;


        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public void Remove(BlockPos pos)
        {
            if (this.server == null)
                throw new InvalidOperationException();
            this.server.parent_blocks.RemoveAll(b => b.Pos == pos);
            this.server.connected_blocks.RemoveAll(b => b.Pos == pos);
            this.connected_coords.RemoveAll(b => b == pos.ToVec3i());
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBytes("connected_blocks_coords", SerializerUtil.Serialize(this.connected_coords));
            tree.SetBytes("state", SerializerUtil.Serialize(this.state));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.connected_coords = SerializerUtil.Deserialize<List<Vec3i>>(
                tree.GetBytes("connected_blocks_coords")
            );
            this.state = SerializerUtil.Deserialize<bool>(tree.GetBytes("state"));
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (this.client == null)
                throw new InvalidOperationException();
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == (int)ServerState.Sync)
                this.client.connected_coords = SerializerUtil.Deserialize<List<Vec3i>>(data);
            else if (packetid == (int)ServerState.Remove)
            {
                var pos = SerializerUtil.Deserialize<Vec3i>(data);
                this.client.connected_coords.Remove(pos);
            }
            else if (packetid == (int)ServerState.ChangeState)
                this.state = SerializerUtil.Deserialize<bool>(data);
        }

        public override void OnBlockBroken(IPlayer byPlayer)
        {
            if (this.client == null)
                return;
            base.OnBlockBroken(byPlayer);
            this.client.api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            this.client.api.Network.SendBlockEntityPacket(Pos, (int)ClientAction.Destroy, SerializerUtil.Serialize(Pos));
        }

        internal void Select()
        {
            if (this.client == null)
                throw new InvalidOperationException();
            this.client.selected = true;
        }

        internal void Unselect()
        {
            if (this.client == null)
                throw new InvalidOperationException();
            this.client.selected = false;
        }

        public void Connect(Logic logic_block)
        {
            if (this.client == null)
                throw new InvalidOperationException();
            if (logic_block.Pos == Pos)
                return;
            this.client.api.Network.SendBlockEntityPacket(Pos, (int)ClientAction.Connect, SerializerUtil.Serialize(logic_block.Pos.ToVec3i()));
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (this.server == null)
                throw new InvalidOperationException();
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
            if (packetid == (int)ClientAction.Connect)
            {
                var coords = SerializerUtil.Deserialize<Vec3i>(data);
                var block = this.server.api.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                if (block != null)
                {
                    if (block is Gate gate_block)
                    {
                        if (gate_block.server == null)
                            throw new InvalidOperationException();
                        foreach (var connected_to_connected in gate_block.server.connected_blocks)
                            if (connected_to_connected.Pos == Pos)
                                return;

                        bool already_connected = false;
                        foreach (var connected in this.server.connected_blocks)
                            if (connected.Pos.ToVec3i() == coords)
                            {
                                already_connected = true;
                                break;
                            }
                        if (already_connected)
                        {
                            this.server.connected_blocks.Remove(gate_block);
                            this.connected_coords.Remove(gate_block.Pos.ToVec3i());
                            gate_block.server.parent_blocks.Remove(this);
                        }
                        else
                        {
                            this.server.connected_blocks.Add(gate_block);
                            this.connected_coords.Add(gate_block.Pos.ToVec3i());
                            gate_block.server.parent_blocks.Add(this);
                            gate_block.Refresh();
                        }

                        this.Sync();
                    }
                }
            }
            else if (packetid == (int)ClientAction.Sync)
            {
                this.Sync();
            }
            else if (packetid == (int)ClientAction.Destroy)
            {
                //this.server.connected_blocks = [];
                this.server.system.BroadcastRemove(this.Pos);
            }
            else if (packetid == (int)ClientAction.GetState)
                this.server.api.Network.SendBlockEntityPacket(fromPlayer as IServerPlayer, Pos, (int)ServerState.ChangeState, SerializerUtil.Serialize<bool>(this.state));
        }

        private void Sync()
        {
            if (this.server == null)
                throw new InvalidOperationException();
            var to_send = SerializerUtil.Serialize(this.connected_coords);
            foreach (IServerPlayer player in this.server.api.Server.Players)
                this.server.api.Network.SendBlockEntityPacket(player as IServerPlayer, Pos, (int)ServerState.Sync, to_send);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreServerAPI sapi)
            {
                List<Gate> connected_blocks = [];
                foreach (var coords in this.connected_coords)
                {
                    var block = sapi.World.BlockAccessor.GetBlockEntity(new BlockPos(coords.X, coords.Y, coords.Z));
                    if (block is Gate logic_block)
                        connected_blocks.Add(logic_block);
                }
                var system = sapi.ModLoader.GetModSystem<LogicBlocksModSystem>();
                system.RegisterLogicBlock(this);
                sapi.Event.EnqueueMainThreadTask(() =>
                {
                    if (this.server == null)
                        throw new InvalidOperationException();
                    foreach (var block in this.server.connected_blocks)
                    {
                        if (block.server == null)
                            throw new InvalidOperationException();
                        block.server.parent_blocks.Add(this);
                    }

                }, $"logicblocks:restoreparents");
                this.server = new ServerResources(sapi, system, connected_blocks, []);
            }
            if (api is ICoreClientAPI capi)
            {
                var system = capi.ModLoader.GetModSystem<LogicBlocksModSystem>();

                var selected_meshref = system.UploadMesh($"logicblocks:selected");
                var connection_false_meshref = system.UploadMesh($"logicblocks:connection_false");
                var connection_true_meshref = system.UploadMesh($"logicblocks:connection_true");
                var triggered_meshref = system.UploadMesh($"logicblocks:{this.Block.Code.GetName()}_triggered");

                capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
                capi.Network.SendBlockEntityPacket(Pos, (int)ClientAction.Sync, SerializerUtil.Serialize(""));

                this.client = new ClientResources(capi, connection_false_meshref, connection_true_meshref, triggered_meshref, selected_meshref, []);

            }
        }

        private void RenderConnection(IRenderAPI rpi, Vec3d cam)
        {
            if (this.client == null)
                return;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 1, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);

            prog.Tex2D = this.client.api.BlockTextureAtlas.AtlasTextures[0].TextureId;

            foreach (Vec3i block in this.client.connected_coords)
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
                    .Scale(0.1f, 0.1f, (float)dist);

                prog.ModelMatrix = modelMat.Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                if (this.state)
                    rpi.RenderMesh(this.client.connection_true_meshref);
                else
                    rpi.RenderMesh(this.client.connection_false_meshref);
            }

            prog.Stop();
        }

        private void RenderTriggered(IRenderAPI rpi, Vec3d cam)
        {
            if (this.client == null || this.client.triggered_meshref == null)
                return;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 1, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);

            prog.Tex2D = this.client.api.BlockTextureAtlas.AtlasTextures[0].TextureId;

            var translation = Pos.ToVec3d() + new Vec3d(0.5, 0.5, 0.5) - cam;

            Matrixf modelMat = new Matrixf()
                .Identity()
                .Translate(translation.X, translation.Y, translation.Z)
                .RotateY((float)Math.PI)
                .Scale(1.001f, 1.001f, 1.001f);

            prog.ModelMatrix = modelMat.Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(this.client.triggered_meshref);

            prog.Stop();
        }

        private void RenderSelected(IRenderAPI rpi, Vec3d cam, float delta)
        {
            if (this.client == null || this.client.triggered_meshref == null)
                return;

            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();

            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaLightIn = new Vec4f(1, 1, 1, 1);
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);

            prog.Tex2D = this.client.api.BlockTextureAtlas.AtlasTextures[0].TextureId;

            var translation = Pos.ToVec3d() + new Vec3d(0.5, 0.5, 0.5) - cam;

            this.client.render_timer += delta;
            float scale_factor = 1.001f + ((float)Math.Sin(this.client.render_timer) + 1.01f) / 10f;

            Matrixf modelMat = new Matrixf()
                .Identity()
                .Translate(translation.X, translation.Y, translation.Z)
                .RotateY((float)Math.PI)
                .Scale(scale_factor, scale_factor, scale_factor);

            prog.ModelMatrix = modelMat.Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(this.client.selected_meshref);

            prog.Stop();
        }


        public void OnRenderFrame(float delta, EnumRenderStage stage)
        {
            if (this.client == null || stage != EnumRenderStage.Opaque)
                return;

            IRenderAPI rpi = this.client.api.Render;
            Vec3d cam = this.client.api.World.Player.Entity.CameraPos;

            if (this.client.selected)
                this.RenderSelected(rpi, cam, delta);

            if (this.client.connected_coords.Count > 0)
                this.RenderConnection(rpi, cam);

            if (this.state)
                this.RenderTriggered(rpi, cam);

        }


        public void Dispose()
        {
            if (this.client != null)
            {
                this.client.connection_false_meshref.Dispose();
                this.client.triggered_meshref.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
