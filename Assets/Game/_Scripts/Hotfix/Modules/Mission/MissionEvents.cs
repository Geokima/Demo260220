namespace Game.Mission
{
    public class MissionListUpdatedEvent { }

    public class MissionClaimedEvent { public string MissionId; }

    public class MissionOperationFailedEvent { public string Reason; }
}