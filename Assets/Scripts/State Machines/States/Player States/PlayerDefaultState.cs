using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDefaultState : PlayerBaseState
{
    public override List<State> bannedStateTransitions { get; set; }

    public PlayerDefaultState(PlayerStateMachine playerStateMachine) : base(playerStateMachine){}

	public override void SetBannedTransitions()
	{
		bannedStateTransitions = new List<State>();

		State[] bannedStates = { stateMachine.playerExitCoverState, stateMachine.playerCoverState };
		bannedStateTransitions.AddRange(bannedStates);
	}

	public override void EnterState()
    {

    }

    public override void UpdateState()
    {
		Aim();

		HasCombatBegun();

		JumpAndGravity();
        GroundedCheck();
        Move();
    }

	public override void LateUpdateState()
	{
        if (CanSwitchToAim())
        {
			stateMachine.CameraRotation(GameManager.Instance.aimXSensitivity, GameManager.Instance.aimYSensitivity, float.MinValue, float.MaxValue);
		}
        else
        {
			stateMachine.CameraRotation(1, 1, float.MinValue, float.MaxValue);
		}
	}

	public override void ExitState()
    {
		//Disable Aim Script
		stateMachine.aimController.enabled = false;
	}


	public override bool CanEnterState()
	{
		return true;
	}

	//Private Methods
	private void HasCombatBegun()
    {
        if (stateMachine.inCombat)
        {
			stateMachine.SwitchState(stateMachine.playerCombatState);
        }
    }
	private void Aim()
    {
		float aimWeight = CanSwitchToAim() ? 1 : 0;

		stateMachine.aimController.SetRigWeight(aimWeight);
		stateMachine.aimController.enabled = CanSwitchToAim();
		playerAnimator.SetBool(animIDAiming, CanSwitchToAim());
    }
	private void Move()
	{
		CharacterController controller = stateMachine.controller;
		// set target speed based on move speed, sprint speed and if sprint is pressed
		float targetSpeed = stateMachine.isSpeedBoostActivated ? stateMachine.SprintSpeed : stateMachine.defaultMoveSpeed;

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (stateMachine.InputMoveValue == Vector2.zero) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;

		float speedOffset = 0.1f;
		//float inputMagnitude = _input.analogMovement ? moveValue.magnitude : 1f;
		float inputMagnitude = 1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			stateMachine.speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * stateMachine.SpeedChangeRate);

			// round speed to 3 decimal places
			stateMachine.speed = Mathf.Round(stateMachine.speed * 1000f) / 1000f;
		}
		else
		{
			stateMachine.speed = targetSpeed;
		}

		//Set Animation Blends
		stateMachine.animationBlend = Mathf.Lerp(stateMachine.animationBlend, targetSpeed, Time.deltaTime * stateMachine.SpeedChangeRate);
		stateMachine.animationBlendVelX = Mathf.Lerp(stateMachine.animationBlendVelX, targetSpeed * stateMachine.InputMoveValue.x, Time.deltaTime * stateMachine.SpeedChangeRate);
		stateMachine.animationBlendVelZ = Mathf.Lerp(stateMachine.animationBlendVelZ, targetSpeed * stateMachine.InputMoveValue.y, Time.deltaTime * stateMachine.SpeedChangeRate);

		// normalise input direction
		Vector3 inputDirection = new Vector3(stateMachine.InputMoveValue.x, 0.0f, stateMachine.InputMoveValue.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		Vector3 targetDirection = Vector3.zero;

        if (CanSwitchToAim())
        {
			//We always want Character to rotate even when movement is 0.
			float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, stateMachine.mainCamera.transform.eulerAngles.y, ref stateMachine.rotationVelocity, stateMachine.aimRotationSmoothTime);
			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		if (stateMachine.InputMoveValue != Vector2.zero)
		{
            if (CanSwitchToAim())
            {
				targetDirection = transform.TransformDirection(inputDirection);
			}
            else
            {
				stateMachine.targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + stateMachine.mainCamera.transform.eulerAngles.y;
				float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, stateMachine.targetRotation, ref stateMachine.rotationVelocity, stateMachine.RotationSmoothTime);

				// rotate to face input direction relative to camera position
				transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
				targetDirection = Quaternion.Euler(0.0f, stateMachine.targetRotation, 0.0f) * Vector3.forward;
			}
		}

		// move the player
		controller.Move(targetDirection.normalized * (stateMachine.speed * Time.deltaTime) + new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);

		//Update Animator
		playerAnimator.SetFloat(animIDSpeed, stateMachine.animationBlend);
		playerAnimator.SetFloat(animIDVelX, stateMachine.animationBlendVelX);
		playerAnimator.SetFloat(animIDVelZ, stateMachine.animationBlendVelZ);
		playerAnimator.SetFloat(animIDMotionSpeed, inputMagnitude);
	}


}
