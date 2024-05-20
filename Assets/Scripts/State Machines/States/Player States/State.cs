using System.Collections.Generic;
using UnityEngine;
public abstract class State
{
    public abstract List<State> bannedStateTransitions { get; set; }
    public abstract void SetBannedTransitions();
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void LateUpdateState();
    public abstract void ExitState();
    public abstract bool CanEnterState();
}
