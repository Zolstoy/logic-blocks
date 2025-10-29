using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    internal class Not : Gate
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            base.server?.api.Event.EnqueueMainThreadTask(() =>
            {
                base.Refresh();
            }, $"refresh:{Pos}");
            base.client?.api.Network.SendBlockEntityPacket(Pos, (int)ClientAction.GetState);

        }
        protected override bool Evaluate()
        {
            if (this.server == null)
                throw new InvalidOperationException();

            if (this.server.parent_blocks.Count == 0)
                return true;
            for (int i = 0; i < this.server.parent_blocks.Count; i++)
                if (this.server.parent_blocks[i].state)
                    return false;

            return true;
        }

    }
}
