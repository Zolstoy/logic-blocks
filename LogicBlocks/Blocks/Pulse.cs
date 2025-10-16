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
    internal class Pulse : Activable
    {
        public override void Activate()
        {
            this.capi?.Network.SendBlockEntityPacket(Pos, 110, SerializerUtil.Serialize(""));
        }

        protected override bool CanTrigger()
        {
            return true;
        }
    }
}
