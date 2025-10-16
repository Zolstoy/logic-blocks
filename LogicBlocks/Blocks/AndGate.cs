using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicBlocks.Blocks
{
    internal class AndGate : Logical
    {
        protected override bool CanTrigger()
        {
            for (int i = 0; i < this.parent_blocks.Count; i++)
            {
                if (!this.parent_blocks[i].state)
                    return false;
            }
            return true;
        }
    }
}
