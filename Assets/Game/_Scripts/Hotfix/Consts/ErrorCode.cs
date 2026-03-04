namespace Game.Consts
{
    /// <summary>
    /// 全局错误码定义
    /// </summary>
    public enum ErrorCode
    {
        Success = 0,

        // 通用错误 (1-99)
        Unknown = 1,
        NetworkError = 2,
        Timeout = 3,
        InvalidParams = 4,

        // 认证错误 (100-199)
        AuthFailed = 100,
        InvalidToken = 101,
        AccountOccupied = 102,

        // 物品与背包 (200-299)
        InventoryFull = 200,
        ItemNotFound = 201,
        MaterialsNotEnough = 202,

        // 建造与网格 (300-399)
        PositionOccupied = 300,
        InvalidBuildingType = 301,
        DistanceTooFar = 302,

        // 交互 (400-499)
        InteractCooldown = 400,
        ToolInappropriate = 401
    }
}
