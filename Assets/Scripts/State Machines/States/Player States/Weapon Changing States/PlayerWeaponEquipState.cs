using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponEquipState : PlayerBaseState
{
    public override List<State> bannedStateTransitions { get; set; }

    public PlayerWeaponEquipState(PlayerStateMachine playerStateMachine) : base(playerStateMachine){}

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();

        State[] bannedStates = {stateMachine.playerWeaponEquipState, stateMachine.playerEnterCoverState, stateMachine.playerExitCoverState };
        bannedStateTransitions.AddRange(bannedStates);
    }


    public override void EnterState()
    {
        //SwitchControls
        ControlsManager.Instance.SwitchControls("MovementOnly");

        hasAnimationStarted = false;
        didAnimationPlayFully = false;

        if (InventoryManager.Instance.IsGroundEquip())
        {
            InventoryManager.Instance.SetEquipNewGun(true);

            if (InventoryManager.Instance.IsGunInvetoryFull())
            {
                stateMachine.SwitchState(stateMachine.playerWeaponDropState);
                return;
            }
            else if (InventoryManager.Instance.CanHolster())
            {
                stateMachine.SwitchState(stateMachine.playerHolsterState);
                return;
            }
            else
            {
                playerAnimator.SetBool(animIDGroundEquip, true);
                InventoryManager.Instance.EquipGun();
                playerAnimator.UpdateCurrentAnimatorOverride(InventoryManager.Instance.GetCurrentEquippedGun());
                playerAnimator.StartGroundEquipRoutine();
            }
        }
        else
        {
            //It's A Storage Equip
            //Animation Triggered early Via an Event
            InventoryManager.Instance.EquipPreviousGun();
            playerAnimator.UpdateCurrentAnimatorOverride(InventoryManager.Instance.GetCurrentEquippedGun());
        }
    }

    public override void UpdateState()
    {
        if(playerAnimator.GetLayerWeight(animatorCoverLayer) < 1)
        {
            BasicPlayerMovement();
            JumpAndGravity();
            GroundedCheck();
        }

        //Due to Delay between UpdateState & Start of Anim, this is necessary
        if (playerAnimator.IsCurrentAnimStateActive("Equip"))
        {
            hasAnimationStarted = true;
        }
        else
        {
            if (hasAnimationStarted)
            {
                didAnimationPlayFully = true;
                InventoryManager.Instance.SetEquipNewGun(false);

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

    public override void LateUpdateState()
    {
        stateMachine.CameraRotation(1, 1, float.MinValue, float.MaxValue);
    }

    public override void ExitState()
    {
        if (!didAnimationPlayFully)
        {
            //Likely Got Hit During Animation.
            InventoryManager.Instance.SetEquipNewGun(false);
        }
        else
        {
            //Re-enable Controls
            ControlsManager.Instance.SwitchControls("Player");
        }

        //Activate Current Gun
        InventoryManager.Instance.ActivateCurrentGun();
    }

    public override bool CanEnterState()
    {
        return true;
    }
}
