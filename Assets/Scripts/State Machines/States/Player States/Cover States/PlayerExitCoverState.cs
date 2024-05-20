using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerExitCoverState : PlayerCoverState
{
    public PlayerExitCoverState(PlayerStateMachine playerStateMachine) : base(playerStateMachine){}

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();

        State[] bannedStates = { stateMachine.playerCoverState, stateMachine.playerEnterCoverState, stateMachine.playerExitCoverState };
        bannedStateTransitions.AddRange(bannedStates);
    }

    public override void EnterState()
    {
        if (stateMachine.aimController.enabled)
        {
            //If Aiming, no animation necessary
            stateMachine.aimController.ShootFromHighCoverSetup(inCoverProhibitedDirection, false, false);
            stateMachine.SwitchState(stateMachine.playerDefaultState);
            playerAnimator.SetLayerWeight(animatorCoverLayer, 0);
            playerAnimator.SetTrigger(animIDExitCover);
            return;
        }


        //Update AutoMover Values.
        stateMachine.speed = 0;
        stateMachine.autoMoverDestination = -transform.forward;

        //ControlsManager.Instance.DisableControls();

        SetMoveDirections(Vector3.zero, Vector3.zero);

        //Reset Character Controller Collider
        SetCharacterControllerConfigs(stateMachine.defaultControllerCenter, stateMachine.defaultControllerRadius, stateMachine.defaultControllerHeight);

        //Update Animator
        playerAnimator.SetTrigger(animIDExitCover);
    }

    public override void UpdateState()
    {
        GeneralMovementMethods();
        //AutoMoverActive Set to true Via Player Animation Event
        if (stateMachine.autoMoverActive)
        {
            if(stateMachine.autoMoverDestination != Vector3.zero)
            {
                stateMachine.controller.Move(stateMachine.autoMoverDestination.normalized * (stateMachine.defaultMoveSpeed * Time.deltaTime) + new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);
            }
        }
        else if (!stateMachine.autoMoverActive && stateMachine.autoMoverDestination == Vector3.zero)
        {
            //Auto Move Ended by Animation Event
            RevertToDefaultOrCombatState();
        }
    }

    public override void LateUpdateState()
    {
        
    }

    public override void ExitState()
    {
        stateMachine.aimController.enabled = false;
        stateMachine.aimController.ShootFromHighCoverSetup(inCoverProhibitedDirection, false, false);
        stateMachine.autoMoverActive = false;

        //Re-enable Player Input.
        //ControlsManager.Instance.EnableCurrentControls();
        //playerAnimator.SetLayerWeight(animatorCoverLayer, 0);
    }


    public override bool CanEnterState()
    {
        return true;
    }


}
