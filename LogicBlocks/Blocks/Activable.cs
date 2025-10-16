using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicBlocks.Blocks
{
    internal abstract class Activable: Logical
    {
        public abstract void Activate();
    }
}
