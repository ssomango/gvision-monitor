using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GVisionWpf.Events.Message.Packet
{
    public class PacketMessage
    {
        public enum EPacketMessageAction
        {
            ShouldTeachNewMarks
        }

        public EPacketMessageAction Action;
    }
}
