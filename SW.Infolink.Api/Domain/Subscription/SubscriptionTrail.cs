using System;
using Newtonsoft.Json;
using SW.PrimitiveTypes;

namespace SW.Infolink.Domain;

public class SubscriptionTrail : BaseEntity<string>, ICreationAudited
{
    private SubscriptionTrail()
    {
    }


    public SubscriptionTrail(SubscriptionTrialCode code, Subscription stateBefore)
    {
        Id = Guid.NewGuid().ToString("N");
        Code = code;
        StateBefore = JsonConvert.SerializeObject(stateBefore);
        SubscriptionId = stateBefore.Id;
    }

    public void SetAfter(Subscription stateAfter)
    {
        StateAfter = JsonConvert.SerializeObject(stateAfter);
    }

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; }

    public SubscriptionTrialCode Code { get; set; }

    public string StateBefore { get; set; }

    public string StateAfter { get; set; }

    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
}