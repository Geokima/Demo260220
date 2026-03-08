using System;
using Game.Config;
using Game.DTOs;
using Framework.Modules.Config;

namespace Game.Gateways
{
    public class ServerContext
    {
        public int UserId;
        public LocalDatabase Db;
        public IConfigSystem Configs;
        
        // 推送委托：分全体推送和单人推送
        public Action<string, object> BroadcastAction; 
        public Action<int, string, object> DirectPushAction;

        public void Push(string msgType, object data)
        {
            DirectPushAction?.Invoke(UserId, msgType, data);
        }
    }
}
