using Game.Base;
using Game.DTOs;

namespace Game.Gateways
{
    public static class PlayerController
    {
        public static PlayerResponse HandleGetResource(ServerContext ctx, BaseRequest req)
        {
            return new PlayerResponse
            {
                Code = 0,
                Data = ctx.Db.GetPlayer(ctx.UserId)
            };
        }
    }
}
