using UnityEngine;
public abstract class StateMachine: MonoBehaviour
{
    protected State currentState;

    private void Update()
    {
        currentState?.UpdateState();
    }

    private void LateUpdate()
    {
        currentState?.LateUpdateState();
    }

    public void SwitchState(State newState)
    {
        if (newState != null && newState.CanEnterState() && CanCurrentStateTransitionToState(newState))
        {
            Debug.Log("Switching to: " + newState.ToString());
            currentState?.ExitState();
            currentState = newState;
            currentState?.EnterState();
        }
        else
        {
            Debug.Log("Current State: " + currentState.ToString() + " Can't Transistion to: " + newState.ToString());
        }
    }


    private bool CanCurrentStateTransitionToState(State nextState)
    {
        if (currentState == null){return true;}

        return !currentState.bannedStateTransitions.Contains(nextState);
    }
}
