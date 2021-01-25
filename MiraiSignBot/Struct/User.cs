using System;
using System.Collections.Generic;
using System.Text;
using NJITSignHelper.SignMsgLib;
using NJITSignHelper.PhyLocation;

namespace MiraiSignBot.Struct
{
    [Serializable]
    public class User
    {
        public long qq;
        public LoginHandler account;
        public Location location;
        public Client cli;
    }
}
