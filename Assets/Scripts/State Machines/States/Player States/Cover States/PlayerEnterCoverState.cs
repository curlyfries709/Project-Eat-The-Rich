using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEnterCoverState : PlayerCoverState
{
    public PlayerEnterCoverState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) {}

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();

        State[] bannedStates = { stateMachine.playerDefaultState, stateMachine.playerExitCoverState, stateMachine.playerEnterCoverState};
        bannedStateTransitions.AddRange(bannedStates);
    }

    public override void EnterState()
    {
        SetCoverType(stateMachine.maxDistanceFromCover);
        BeginMoveToCover();
    }

    public override void UpdateState()
    {
        GeneralMovementMethods();

        if (stateMachine.autoMoverActive)
        {
            Vector3 moveDirection = (stateMachine.autoMoverDestination - transform.position).normalized;

            if (Vector3.Distance(transform.position, stateMachine.autoMoverDestination) > stateMachine.autoMoverStoppingDistance)
            {
                stateMachine.animationBlend = stateMachine.SprintSpeed;
                stateMachine.controller.Move(moveDirection * stateMachine.SprintSpeed * Time.deltaTime + new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else
            {
                //Change States on arrival
                Debug.Log("Switching to Cover State");
                stateMachine.SwitchState(stateMachine.playerCoverState);
            }

            //Update Animator
            playerAnimator.SetFloat(animIDSpeed, stateMachine.animationBlend);
            playerAnimator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    public override void LateUpdateState()
    {

    }

    public override void ExitState()
    {
        Vector3 perpDirection = Vector3.Cross(inCoverMoveDirection, Vector3.up);
        TeleportCharacter(stateMachine.autoMoverDestination + (-perpDirection.normalized * stateMachine.coverOffset));

        playerAnimator.SetTrigger(animIDEnterCover);
        
        //Exit AutoMove
        stateMachine.autoMoverActive = false;
        stateMachine.autoMoverDestination = Vector3.zero;

        //Re-enable Player Input.
        ControlsManager.Instance.EnableCurrentControls();
    }


    public override bool CanEnterState()
    {
        return IsNearCover();
    }


    private void BeginMoveToCover()
    {
        //Disable Player Input
        //ControlsManager.Instance.DisableControls();
        SetMoveDirections(coverSurfaceDirection, Vector3.zero);
        stateMachine.autoMoverActive = true;
        stateMachine.autoMoverDestination = coverHitPoint;
        
        //Set Animations
        playerAnimator.SetLayerWeight(animatorCoverLayer, 1);
        playerAnimator.SetBool(animIDInHighCover, inHighCover);
    }

}
