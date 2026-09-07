using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using SW.PrimitiveTypes;


namespace SW.Bitween.Domain;

public class Notifier(string name) : BaseEntity
{
    public string Name { get; set; } = name;
    public bool RunOnSuccessfulResult { get;  set; }
    public bool RunOnBadResult { get;  set; }
    public bool RunOnFailedResult { get; set; }
    public string HandlerId { get; set; }
    public bool Inactive { get; set; } = false;
    public IReadOnlyDictionary<string, string> HandlerProperties { get; private set; }

    public int[] RunOnSubscriptions { get; set; }

    public void Update(string name, bool runOnSuccessfulResult, bool runOnBadResult, bool runOnFailedResult, string handlerId,bool inactive, int[] runOnSubscriptions)
    {
        Name = name;
        RunOnSuccessfulResult = runOnSuccessfulResult;
        RunOnBadResult = runOnBadResult;
        RunOnFailedResult = runOnFailedResult;
        HandlerId = handlerId;
        Inactive = inactive;
        RunOnSubscriptions = runOnSubscriptions;
    }
    public void SetDictionaries(
        IReadOnlyDictionary<string, string> handler
    )
    {
        HandlerProperties = handler;
    }
        
        
        
        
}