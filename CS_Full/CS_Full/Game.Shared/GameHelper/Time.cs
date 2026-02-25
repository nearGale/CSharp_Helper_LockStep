using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public partial class GameHelper_Common
    {
        public static Timestamp GetCurTimeStamp()
        {
            DateTime loginTimeUtc = DateTime.UtcNow;
            Timestamp protoLoginTime = Timestamp.FromDateTime(loginTimeUtc);
            return protoLoginTime;
        }
    }
}
