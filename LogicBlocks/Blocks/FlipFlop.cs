﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    internal class FlipFlop : Gate
    {
        protected override bool Evaluate()
        {
            if (this.server == null)
                throw new InvalidOperationException();

            if (this.server.parent_blocks.Count == 0)
                return this.state;
            for (int i = 0; i < this.server.parent_blocks.Count; i++)
                if (this.server.parent_blocks[i].state)
                    return !state;

            return this.state;
        }
    }
}
