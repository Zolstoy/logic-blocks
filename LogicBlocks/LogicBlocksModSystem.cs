using LogicBlocks.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace LogicBlocks
{
    public class LogicBlocksModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            // Fix: Use api.CurrentMod.Info.ModID instead of Mod.Info.ModID
            api.RegisterBlockClass(Mod.Info.ModID + ".gate_and", typeof(BlockGateAnd));
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("logicblocks:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("logicblocks:hello"));
        }
    }
}
