using UnityEngine;
using UnityEngine.InputSystem;
using System;


/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */


public class PlayerController : MonoBehaviour, ICharacterMover
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 2.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 5.335f;
	[Tooltip("How fast the character turns to face movement direction")]
	[Range(0.0f, 0.3f)]
	public float RotationSmoothTime = 0.12f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0.50f;
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.28f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Offsets")]
	[SerializeField] float autoMoverStoppingDistance = 1f;
	[SerializeField] float coverOffset = 0.25f;

	[Header("Character Controller Configs")]
	[SerializeField] Vector3 crouchControllerCenter;
	[SerializeField] float crouchControllerRadius;
	[SerializeField] float crouchControllerHeight;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;
	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 70.0f;
	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -30.0f;
	[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
	public float CameraAngleOverride = 0.0f;
	[Tooltip("For locking the camera position on all axis")]
	public bool LockCameraPosition = false;
	[Header("Invert Cam Controls")]
	[SerializeField] bool invertCamX = false;
	[SerializeField] bool invertCamY = false;

	// cinemachine
	private float _cinemachineTargetYaw;
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _animationBlend;
	private float _targetRotation = 0.0f;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _terminalVelocity = 53.0f;
	

	//Input Values
	public Vector2 moveValue { get; private set; }
	public Vector2 lookValue { get; private set; }
	public bool jump { get; private set; }
	public bool sprint { get; private set; }

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	// animation IDs
	private int _animIDSpeed;
	private int _animIDGrounded;
	private int _animIDJump;
	private int _animIDFreeFall;
	private int _animIDMotionSpeed;
	private int animIDEnterCover;
	private int animIDInHighCover;
	private int animIDExitCover;

	//Cover Variables
	public bool inCover { get; set; }
	public bool inHighCover { get; set; }
	public Vector3 inCoverMoveDirection { get; set; }
	public Vector3 inCoverProhibitedDirection { get; set; }

	//Auto Mover Variables
	Vector3 autoMoverTargetPos;
	private bool autoMoverActive = false;

	//Character Controller Configs
	Vector3 defaultControllerCenter;
	float defaultControllerRadius;
	float defaultControllerHeight;

	//Caches
	private PlayerAnimator playerAnimator;
	private CharacterController _controller;
	private GameObject _mainCamera;
	private PlayerInput playerInput;

	private const float _threshold = 0.01f;

	//Events
	public Action InteractWithObject; 
	

    private void Awake()
	{
		// get a reference to our main camera
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

		playerAnimator = GetComponent<PlayerAnimator>();
		_controller = GetComponent<CharacterController>();

		//Set Character Controller Default Values
		defaultControllerCenter = _controller.center;
		defaultControllerRadius = _controller.radius;
		defaultControllerHeight = _controller.height;
	}

	private void OnEnable()
	{
		playerInput = ControlsManager.Instance.GetPlayerInput();
		playerInput.onActionTriggered += OnMove;
		playerInput.onActionTriggered += OnLook;
		playerInput.onActionTriggered += OnSprint;
		playerInput.onActionTriggered += OnInteract;
		playerInput.onActionTriggered += OnDrop;
		playerInput.onActionTriggered += OnQuickSwitch;
	}

	private void Start()
	{
		AssignAnimationIDs();
		// reset our timeouts on start
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
	}
        

    private void Update()
	{
		print("Player Controller running");
		JumpAndGravity();
		GroundedCheck();
		MovePlayer();
		CameraRotation();
	}

	private void LateUpdate()
	{
		//CameraRotation();
	}



	
	private void AssignAnimationIDs()
	{
		_animIDSpeed = Animator.StringToHash("Speed");
		_animIDGrounded = Animator.StringToHash("Grounded");
		_animIDJump = Animator.StringToHash("Jump");
		_animIDFreeFall = Animator.StringToHash("FreeFall");
		_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		animIDEnterCover = Animator.StringToHash("EnterCover");
		animIDInHighCover = Animator.StringToHash("InHighCover");
		animIDExitCover = Animator.StringToHash("ExitCover");
	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
		Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

		playerAnimator.SetBool(_animIDGrounded, Grounded);
	}

	private void CameraRotation()
	{
		// if there is an input and camera position is not fixed
		if (lookValue.sqrMagnitude >= _threshold && !LockCameraPosition)
		{
			bool isCurrentDeviceMouse = ControlsManager.Instance.IsCurrentControlScheme("KeyboardMouse");
			float deltaTimeMultiplier = isCurrentDeviceMouse ? 1.0f : Time.deltaTime;

			if (invertCamX)
            {
				_cinemachineTargetYaw += -lookValue.x * deltaTimeMultiplier ;
			}
            else
            {
				_cinemachineTargetYaw += lookValue.x * deltaTimeMultiplier ;
			}


            if (invertCamY)
            {
				_cinemachineTargetPitch += -lookValue.y * deltaTimeMultiplier ;
			}
            else
            {
				_cinemachineTargetPitch += lookValue.y * deltaTimeMultiplier ;
			}
			
		}

		// clamp our rotations so our values are limited 360 degrees
		_cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
		_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

		// Cinemachine will follow this target
		CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
	}

	private void MovePlayer()
    {
		if (inCover && !autoMoverActive)
		{
			InCoverMove();
		}
		else if (inCover && autoMoverActive)
		{
			MoveToCover();
		}
		else if (autoMoverActive)
        {
			AutoMoveCharacter();
        }
		else
		{
			Move();
		}
	}

    private void Move()
	{
		// set target speed based on move speed, sprint speed and if sprint is pressed
		float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (moveValue == Vector2.zero) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float speedOffset = 0.1f;
		//float inputMagnitude = _input.analogMovement ? moveValue.magnitude : 1f;
		float inputMagnitude = 1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}
		_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

		// normalise input direction
		Vector3 inputDirection = new Vector3(moveValue.x, 0.0f, moveValue.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		if (moveValue != Vector2.zero)
		{
			_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
			float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

			// rotate to face input direction relative to camera position
			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}


		Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

		// move the player
		_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

		//Update Animator
		playerAnimator.SetFloat(_animIDSpeed, _animationBlend);
		playerAnimator.SetFloat(_animIDMotionSpeed, inputMagnitude);
		

	}

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			playerAnimator.SetBool(_animIDJump, false);
			playerAnimator.SetBool(_animIDFreeFall, false);

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = -2f;
			}

			// Jump
			if (jump && _jumpTimeoutDelta <= 0.0f)
			{
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

				// update animator if using character
				playerAnimator.SetBool(_animIDJump, true);
			}

			// jump timeout
			if (_jumpTimeoutDelta >= 0.0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			// reset the jump timeout timer
			_jumpTimeoutDelta = JumpTimeout;

			// fall timeout
			if (_fallTimeoutDelta >= 0.0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				playerAnimator.SetBool(_animIDFreeFall, true);
			}

			// if we are not grounded, do not jump
			jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}


	//Cover Methods
	public void BeginMoveToCover(Vector3 targetPos)
	{
		//Disable Player Input
		ControlsManager.Instance.DisableControls();

		inCover = true;
		autoMoverActive = true;
		autoMoverTargetPos = targetPos;

		playerAnimator.SetLayerWeight(1, 1);
		playerAnimator.SetBool(animIDInHighCover, inHighCover);
	}

	private void MoveToCover()
    {
		Vector3 moveDirection = (autoMoverTargetPos - transform.position).normalized;

        if (Vector3.Distance(transform.position, autoMoverTargetPos) > autoMoverStoppingDistance)
        {
			_animationBlend = SprintSpeed;
			_controller.Move(moveDirection * SprintSpeed * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}
        else
        {
			Vector3 perpDirection = Vector3.Cross(inCoverMoveDirection, Vector3.up);
			TeleportCharacter(autoMoverTargetPos + (-perpDirection.normalized * coverOffset));

			playerAnimator.SetTrigger(animIDEnterCover);
			//Re-enable Player Input.
			ControlsManager.Instance.EnableCurrentControls();

			autoMoverActive = false;
			autoMoverTargetPos = Vector3.zero;
		}

		//Update Animator
		playerAnimator.SetFloat(_animIDSpeed, _animationBlend);
		playerAnimator.SetFloat(_animIDMotionSpeed, 1f);

	}

	public void ExitCover()
    {
		_speed = 0;
		ControlsManager.Instance.DisableControls();

		inCover = false;
		inCoverMoveDirection = Vector3.zero;
		inCoverProhibitedDirection = Vector3.zero;

		//Reset Character Controller Collider
		SetCharacterControllerConfigs(defaultControllerCenter, defaultControllerRadius, defaultControllerHeight);

		//Update Animator
		playerAnimator.SetTrigger(animIDExitCover);
		//Layer Weight Reset In StateMachine Behaviour
	}

	private void InCoverMove()
	{
		// set target speed based on move speed, sprint speed and if sprint is pressed
		float targetSpeed = MoveSpeed;

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// if there is no left or right input, set the target speed to 0
		if (moveValue.x == 0) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float speedOffset = 0.1f;
		//float inputMagnitude = _input.analogMovement ? moveValue.magnitude : 1f;
		float inputMagnitude = 1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}

		// normalise input direction
		//Vector3 inputDirection = new Vector3(moveValue.x, 0.0f, moveValue.y).normalized;

		//Update Rotation
		Vector3 perpDirection = Vector3.Cross(inCoverMoveDirection, Vector3.up);
		//Quaternion lookRotation = Quaternion.LookRotation(perpDirection);
		//transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, RotationSmoothTime * Time.deltaTime);
		transform.forward = perpDirection;

		Vector3 moveDirection = inCoverMoveDirection.normalized * moveValue.x;

        // move the player
        if (moveDirection != inCoverProhibitedDirection.normalized)
        {
			_controller.Move(moveDirection * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}
        else
        {
			targetSpeed = 0f;
        }

		_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed * moveValue.x, Time.deltaTime * SpeedChangeRate);

		//Update Character Controller
		if (inHighCover)
        {
			SetCharacterControllerConfigs(defaultControllerCenter, defaultControllerRadius, defaultControllerHeight);
        }
        else
        {
			SetCharacterControllerConfigs(crouchControllerCenter, crouchControllerRadius, crouchControllerHeight);
		}
		
		playerAnimator.SetFloat(_animIDSpeed, _animationBlend);
		playerAnimator.SetFloat(_animIDMotionSpeed, 1f);
		playerAnimator.SetBool(animIDInHighCover, inHighCover);

		

	}

	private void SetCharacterControllerConfigs(Vector3 center, float radius, float height)
    {
		_controller.center = center;
		_controller.radius = radius;
		_controller.height = height;
    }

	//AutoMove
	private void TeleportCharacter(Vector3 destination)
    {
		_controller.enabled = false;
		transform.position = destination;
		_controller.enabled = true;
    }


	private void BeginAutoMove(Vector3 direction, float speed)
    {
		autoMoverActive = true;
		autoMoverTargetPos = direction;
		_speed = speed;
    }

	private void AutoMoveCharacter()
    {
		_controller.Move(autoMoverTargetPos.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

	private void EndAutoMove()
    {
		autoMoverActive = false;
    }

	//Called By Animation Event
	private void BeginExitCoverAutoMovement()
    {
		BeginAutoMove(-transform.forward, MoveSpeed);
    }

	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;
			
		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
	}

	private void OnDisable()
	{
		playerInput.onActionTriggered -= OnMove;
		playerInput.onActionTriggered -= OnLook;
		playerInput.onActionTriggered -= OnSprint;
		playerInput.onActionTriggered -= OnInteract;
		playerInput.onActionTriggered -= OnDrop;
		playerInput.onActionTriggered -= OnQuickSwitch;
	}

	//Inputs
	private void OnMove(InputAction.CallbackContext context)
	{
		if (context.action.name != "Move") return;

		moveValue = context.ReadValue<Vector2>();


	}

	private void OnLook(InputAction.CallbackContext context)
    {
		if (context.action.name != "Look") return;

		lookValue = context.ReadValue<Vector2>();
		
	}


	private void OnSprint(InputAction.CallbackContext context)
    {
		if (context.action.name != "Sprint") return;

        if (context.performed)
        {
			sprint = true;
        }
		else if (context.canceled)
        {
			sprint = false;
        }
    }

	private void OnInteract(InputAction.CallbackContext context)
	{
		if (context.action.name != "Interact") return;

        if (context.performed)
        {
			InteractWithObject();
		}
	}

	private void OnDrop(InputAction.CallbackContext context)
	{
		if (context.action.name != "Drop") return;

		if (context.performed)
		{
            if (InventoryManager.Instance.GetCurrentEquippedGun())
            {
				//InventoryManager.Instance.DropGun(InventoryManager.Instance.GetCurrentEquippedGun().transform.GetSiblingIndex(), true);
			}
		}
	}

	private void OnQuickSwitch(InputAction.CallbackContext context)
    {
		if (context.action.name != "QuickSwitch") return;

		if (context.performed)
		{
			if (InventoryManager.Instance.GetCurrentEquippedGun())
			{
				InventoryManager.Instance.QuickSwitch();
			}
		}
	}
}