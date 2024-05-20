using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHolsterState : PlayerBaseState
{
    public override List<State> bannedStateTransitions { get; set; }

    public PlayerHolsterState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();

        State[] bannedStates = { stateMachine.playerWeaponDropState, stateMachine.playerEnterCoverState, stateMachine.playerExitCoverState };
        bannedStateTransitions.AddRange(bannedStates);
    }


    public override void EnterState()
    {
        //SwitchControls
        ControlsManager.Instance.SwitchControls("MovementOnly");

        didAnimationPlayFully = false;
        hasAnimationStarted = false;
        playerAnimator.SetTrigger(animIDHolster);
    }

    public override void UpdateState()
    {
        if (playerAnimator.GetLayerWeight(animatorCoverLayer) < 1)
        {
            BasicPlayerMovement();
            JumpAndGravity();
            GroundedCheck();
        }

        //Due to Delay between UpdateState & Start of Anim, this is necessary
        if (playerAnimator.IsCurrentAnimStateActive("Holster"))
        {
            hasAnimationStarted = true;
        }
        else
        {
            if (hasAnimationStarted)
            {
                HolsterStateComplete();
            }
        }
    }

    public override void LateUpdateState()
    {
        stateMachine.CameraRotation(1, 1, float.MinValue, float.MaxValue);
    }

    public override void ExitState()
    {
        if (!didAnimationPlayFully)
        {
            //Likely Got Hit During Animation.
            InventoryManager.Instance.ActivateCurrentGun();
        }

        playerAnimator.UpdateCurrentAnimatorOverride(InventoryManager.Instance.GetCurrentEquippedGun());
    }

    private void HolsterStateComplete()
    {
        didAnimationPlayFully = true;
        stateMachine.SwitchState(stateMachine.playerWeaponEquipState);
    }


    public override bool CanEnterState()
    {
        return InventoryManager.Instance.GetCurrentEquippedGun();
    }
}
