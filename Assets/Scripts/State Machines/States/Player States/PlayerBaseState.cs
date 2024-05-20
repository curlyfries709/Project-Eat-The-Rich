using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;

	// animation IDs
	protected int animIDVelX;
	protected int animIDVelZ;
	protected int animIDSpeed;
	protected int animIDMotionSpeed;

	protected int animIDAiming;
	protected int animIDGrounded;
	protected int animIDJump;
	protected int animIDFreeFall;

	protected int animIDEnterCover;
	protected int animIDInHighCover;
	protected int animIDExitCover;

	protected int animIDStorageEquip;
	protected int animIDGroundEquip;
	protected int animIDHolster;
	protected int animIDDropGun;

	//Index
	protected int animatorCoverLayer = 1;

	//Jump
	private bool jump;

	// timeout deltatime
	private float jumpTimeoutDelta;
	private float fallTimeoutDelta;

	//Camera Rotation Variables
	//private float cinemachineTargetYaw;
	//private float cinemachineTargetPitch;

	//private const float _threshold = 0.01f;

	//Animation bools
	protected bool hasAnimationStarted = false;
	protected bool didAnimationPlayFully = false;

	//Caches
	protected PlayerAnimator playerAnimator;
	protected Transform transform;

	public PlayerBaseState(PlayerStateMachine playerStateMachine)
    {
        stateMachine = playerStateMachine;
		transform = stateMachine.transform;
		playerAnimator = stateMachine.playerAnimator;
		AssignAnimationIDs();

		// reset our timeouts on start
		jumpTimeoutDelta = stateMachine.JumpTimeout;
		fallTimeoutDelta = stateMachine.FallTimeout;
	}

	//Jump & Gravity
	protected void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - stateMachine.GroundedOffset, transform.position.z);
		stateMachine.Grounded = Physics.CheckSphere(spherePosition, stateMachine.GroundedRadius, stateMachine.GroundLayers, QueryTriggerInteraction.Ignore);

		playerAnimator.SetBool(animIDGrounded, stateMachine.Grounded);
	}

	protected void JumpAndGravity()
	{
		if (stateMachine.Grounded)
		{
			// reset the fall timeout timer
			fallTimeoutDelta = stateMachine.FallTimeout;

			playerAnimator.SetBool(animIDJump, false);
			playerAnimator.SetBool(animIDFreeFall, false);

			// stop our velocity dropping infinitely when grounded
			if (stateMachine.verticalVelocity < 0.0f)
			{
				stateMachine.verticalVelocity = -2f;
			}

			// Jump
			if (jump && jumpTimeoutDelta <= 0.0f)
			{
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.JumpHeight * -2f * stateMachine.Gravity);

				// update animator if using character
				playerAnimator.SetBool(animIDJump, true);
			}

			// jump timeout
			if (jumpTimeoutDelta >= 0.0f)
			{
				jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			// reset the jump timeout timer
			jumpTimeoutDelta = stateMachine.JumpTimeout;

			// fall timeout
			if (fallTimeoutDelta >= 0.0f)
			{
				fallTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				playerAnimator.SetBool(animIDFreeFall, true);
			}

			// if we are not grounded, do not jump
			jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (stateMachine.verticalVelocity < stateMachine.terminalVelocity)
		{
			stateMachine.verticalVelocity += stateMachine.Gravity * Time.deltaTime;
		}
	}

	protected void BasicPlayerMovement()
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
		stateMachine.animationBlend = Mathf.Lerp(stateMachine.animationBlend, targetSpeed, Time.deltaTime * stateMachine.SpeedChangeRate);

		// normalise input direction
		Vector3 inputDirection = new Vector3(stateMachine.InputMoveValue.x, 0.0f, stateMachine.InputMoveValue.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		if (stateMachine.InputMoveValue != Vector2.zero)
		{
			stateMachine.targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + stateMachine.mainCamera.transform.eulerAngles.y;
			float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, stateMachine.targetRotation, ref stateMachine.rotationVelocity, stateMachine.RotationSmoothTime);

			// rotate to face input direction relative to camera position
			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		Vector3 targetDirection = Quaternion.Euler(0.0f, stateMachine.targetRotation, 0.0f) * Vector3.forward;

		// move the player
		controller.Move(targetDirection.normalized * (stateMachine.speed * Time.deltaTime) + new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);

		//Update Animator
		playerAnimator.SetFloat(animIDSpeed, stateMachine.animationBlend);
		playerAnimator.SetFloat(animIDMotionSpeed, inputMagnitude);
	}

	//Switch States
	protected void RevertToDefaultOrCombatState()
    {
        if (stateMachine.inCombat)
        {
			stateMachine.SwitchState(stateMachine.playerCombatState);
        }
        else
        {
			stateMachine.SwitchState(stateMachine.playerDefaultState);
        }
    }

	//Character Controller Manipulation
	protected void TeleportCharacter(Vector3 destination)
	{
		stateMachine.controller.enabled = false;
		transform.position = destination;
		stateMachine.controller.enabled = true;
	}
	protected void SetCharacterControllerConfigs(Vector3 center, float radius, float height)
	{
		stateMachine.controller.center = center;
		stateMachine.controller.radius = radius;
		stateMachine.controller.height = height;
	}

	//Animations
	protected void AssignAnimationIDs()
	{
		animIDAiming = Animator.StringToHash("Aiming");


		animIDSpeed = Animator.StringToHash("Speed");
		animIDVelX = Animator.StringToHash("VelocityX");
		animIDVelZ = Animator.StringToHash("VelocityZ");

		animIDGrounded = Animator.StringToHash("Grounded");
		animIDJump = Animator.StringToHash("Jump");
		animIDFreeFall = Animator.StringToHash("FreeFall");
		animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		animIDEnterCover = Animator.StringToHash("EnterCover");
		animIDInHighCover = Animator.StringToHash("InHighCover");
		animIDExitCover = Animator.StringToHash("ExitCover");

		animIDStorageEquip = Animator.StringToHash("StorageEquip");
		animIDGroundEquip = Animator.StringToHash("GroundEquip");
		animIDHolster = Animator.StringToHash("Holster");
		animIDDropGun = Animator.StringToHash("DropGun");
	}

	//Getters
	protected bool CanSwitchToAim()
    {
		return stateMachine.isAimTriggered && InventoryManager.Instance.GetCurrentEquippedGun();
    }

	protected bool canDash()
    {
		return stateMachine.isSpeedBoostActivated && 
			stateMachine.superSpeedController.IsSpeedBoostAvailable() 
			&& stateMachine.inCombat 
			&& stateMachine.InputMoveValue != Vector2.zero;
    }

}
