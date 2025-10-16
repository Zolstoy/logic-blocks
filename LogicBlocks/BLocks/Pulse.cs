using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace LogicBlocks.BLocks
{

    internal class Pulse: Block
    {
        public List<Block> connected_blocks;

        public Pulse() { 
            connected_blocks = new List<Block>();
        }
    }
}
