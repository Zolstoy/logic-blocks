using LogicBlocks.Blocks;
using LogicBlocks.Items;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace LogicBlocks
{
    public class LogicBlocksModSystem : ModSystem
    {

        private ICoreAPI? api;
        private readonly List<Logic> logic_blocks;
        private readonly Dictionary<String, MeshRef> meshes;

        public LogicBlocksModSystem()
        {
            logic_blocks = [];
            meshes = [];
        }            

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            this.api.RegisterItemClass(this.Mod.Info.ModID + ".connector", typeof(Connector));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".pulse", typeof(Pulse));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".switch", typeof(Switch));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".andgate", typeof(And));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".orgate", typeof(Or));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".notgate", typeof(Not));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".flipflop", typeof(FlipFlop));
        }

        public void RegisterLogicBlock(Logic logic_block)
        {
            this.logic_blocks.Add(logic_block);
        }

        public void BroadcastRemove(BlockPos pos)
        {
            if (api is ICoreServerAPI sapi)
            {
                var to_send = SerializerUtil.Serialize(pos.ToVec3i());
                foreach (IServerPlayer player in sapi.Server.Players)
                {
                    foreach (var logic_block in logic_blocks)
                    {
                        logic_block.Remove(pos);
                        sapi?.Network.SendBlockEntityPacket(player as IServerPlayer, logic_block.Pos, (int)Logic.ServerState.Remove, to_send);
                    }
                    
                }

                this.logic_blocks.RemoveAll(b => b.Pos == pos);
            }
        }

        internal MeshRef UploadMesh(string key)
        {
            if (this.api is not ICoreClientAPI capi)
                throw new InvalidOperationException();

            if (!this.meshes.TryGetValue(key, out MeshRef? value))
            {

                Block block = capi.World.GetBlock(new AssetLocation(key));
                capi.Tesselator.TesselateBlock(block, out MeshData mesh);
                value = capi.Render.UploadMesh(mesh);
                this.meshes[key] = value;
            }

            return value;
        }
    }
}
