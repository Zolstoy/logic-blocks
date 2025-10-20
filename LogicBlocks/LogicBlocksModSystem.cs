using LogicBlocks.Blocks;
using LogicBlocks.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace LogicBlocks
{
    public class LogicBlocksModSystem : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".connector", typeof(Connector));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".pulse", typeof(Pulse));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
        }

    }
}
