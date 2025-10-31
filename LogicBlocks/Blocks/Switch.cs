﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace LogicBlocks.Blocks
{
    internal class Switch : Activator
    {
        public override void Interact()
        {
            if (this.client == null)
                throw new InvalidOperationException();
            this.client.api.Network.SendBlockEntityPacket(Pos, (int)ClientAction.Trigger, SerializerUtil.Serialize(""));
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            base.client?.api.Network.SendBlockEntityPacket(Pos, (int)ClientAction.GetState);
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);

            if (base.server == null)
                throw new InvalidOperationException();

            if (packetid == (int)ClientAction.Trigger)
            {
                this.state = !this.state;
                foreach (IServerPlayer player in this.server.api.Server.Players)
                    this.server.api.Network.SendBlockEntityPacket(player as IServerPlayer, Pos, (int)ServerState.ChangeState, SerializerUtil.Serialize(this.state));
                for (int i = 0; i < base.server.connected_blocks.Count; i++)
                {
                    base.server.connected_blocks[i].Refresh();
                }
            }
        }

    }
}
