using LogicBlocks.BLocks;
using LogicBlocks.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace LogicBlocks
{
    public class LogicBlocksModSystem : ModSystem
    {

        //static List<Item> logicBlocks;

        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".connector", typeof(Connector));
            api.RegisterBlockClass(Mod.Info.ModID + ".pulse", typeof(Pulse));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
        }

    }
}
