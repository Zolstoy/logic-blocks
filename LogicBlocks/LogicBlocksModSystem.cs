using LogicBlocks.Blocks;
using LogicBlocks.Items;
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
        private readonly List<Logical> logic_blocks;

        public LogicBlocksModSystem()
        {
            logic_blocks = [];
        }            

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            this.api.RegisterItemClass(this.Mod.Info.ModID + ".connector", typeof(Connector));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".pulse", typeof(Pulse));
            this.api.RegisterBlockEntityClass(this.Mod.Info.ModID + ".andgate", typeof(AndGate));
        }

        public void RegisterLogicBlock(Logical logic_block)
        {
            this.logic_blocks.Add(logic_block);
        }

        public void BroadcastRemove(BlockPos pos)
        {
            if (api is ICoreServerAPI sapi)
            {
                var to_send = SerializerUtil.Serialize(pos);
                foreach (IServerPlayer player in sapi.Server.Players)
                {
                    foreach (var logic_block in logic_blocks)
                    {
                        logic_block.Remove(pos);
                        sapi?.Network.SendBlockEntityPacket(player as IServerPlayer, logic_block.Pos, 98, to_send);
                    }
                    
                }

                this.logic_blocks.RemoveAll(b => b.Pos == pos);
            }
        }

    }
}
