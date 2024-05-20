using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponDropState : PlayerBaseState
{
    public override List<State> bannedStateTransitions { get; set; }

    public PlayerWeaponDropState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();

        State[] bannedStates = { stateMachine.playerWeaponDropState, stateMachine.playerEnterCoverState, stateMachine.playerExitCoverState, stateMachine.playerHolsterState }; 
        bannedStateTransitions.AddRange(bannedStates);
    }

    public override void EnterState()
    {
        //SwitchControls
        ControlsManager.Instance.SwitchControls("MovementOnly");

        hasAnimationStarted = false;
        playerAnimator.SetTrigger(animIDDropGun);
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
        if (playerAnimator.IsCurrentAnimStateActive("Drop"))
        {
            hasAnimationStarted = true;
        }
        else
        {
            if (hasAnimationStarted)
            {
                WeaponDropStateComplete();
            }
        }

    }

    public override void LateUpdateState()
    {
        stateMachine.CameraRotation(1, 1, float.MinValue, float.MaxValue);
    }

    public override void ExitState()
    {
        if (!InventoryManager.Instance.ShouldEquipGunAfterDropping())
        {
            //Re-enable Controls if player Unarmed
            ControlsManager.Instance.SwitchControls("Player");
        }

        playerAnimator.UpdateCurrentAnimatorOverride(InventoryManager.Instance.GetCurrentEquippedGun());
    }

    public override bool CanEnterState()
    {
        return InventoryManager.Instance.GetCurrentEquippedGun();
    }

    private void WeaponDropStateComplete()
    {
        if (InventoryManager.Instance.ShouldEquipGunAfterDropping())
        {
            stateMachine.SwitchState(stateMachine.playerWeaponEquipState);
        }
        else
        {
            if (playerAnimator.GetLayerWeight(animatorCoverLayer) < 1)
            {
                RevertToDefaultOrCombatState();
            }
            else
            {
                stateMachine.SwitchState(stateMachine.playerCoverState);
            }
        }
    }
}
