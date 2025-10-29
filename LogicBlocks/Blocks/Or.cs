using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicBlocks.Blocks
{
    internal class Or : Gate
    {
        protected override bool Evaluate()
        {
            if (this.server == null)
                throw new InvalidOperationException();

            if (this.server.parent_blocks.Count == 0)
                return false;

            for (int i = 0; i < this.server.parent_blocks.Count; i++)
            {
                if (this.server.parent_blocks[i].state)
                    return true;
            }

            return false;
        }
    }
}
