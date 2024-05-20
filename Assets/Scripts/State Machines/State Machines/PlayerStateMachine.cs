using UnityEngine.InputSystem;
using UnityEngine;
using System;

public class PlayerStateMachine : StateMachine
{
    [Header("Speeds")]
	[Tooltip("Move speed of the character in m/s")]
	public float defaultMoveSpeed = 2.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 5.335f;
	public float inCombatMoveSpeed = 4f;
	public float superSpeed = 20f;
	[Tooltip("How fast the character turns to face movement direction")]
	[Range(0.0f, 0.3f)]
	public float RotationSmoothTime = 0.12f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;

	[Space(10)]
	public bool inCombat = false;

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

	[Header("Aim Config")]
	[Range(0.0f, 0.3f)]
	public float aimRotationSmoothTime = 0.2f;

	[Header("Character Controller Configs")]
	public Vector3 crouchControllerCenter;
	public float crouchControllerRadius;
	public float crouchControllerHeight;

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

	[Header("Cover Data")]
	public float maxDistanceFromCover = 2f;
	public float horizontalCoverDetectorLength = 1f;

	[Space(10)]
	public Transform highCoverDetectionTransform;
	public Transform leftCoverDetectionTransform;
	public Transform rightCoverDetectionTransform;

	[Header("Offsets")]
	public float autoMoverStoppingDistance = 1f;
	public float coverOffset = 0.25f;


	//Character Controller Configs
	public Vector3 defaultControllerCenter { get; set; }
	public float defaultControllerRadius { get; set; }
	public float defaultControllerHeight { get; set; }

	//AUTO MOVER VARIABLES
	[HideInInspector] public bool autoMoverActive = false;
	[HideInInspector] public Vector3 autoMoverDestination = Vector3.zero;

	//Input Values
	public Vector2 InputMoveValue { get; private set; }
	public Vector2 InputLookValue { get; private set; }
	public bool InputJump { get; private set; }
	public bool isSpeedBoostActivated { get; private set; }
	public bool isAimTriggered { get; private set; }

	//Camera Rotation Variables
	private float cinemachineTargetYaw;
	private float cinemachineTargetPitch;

	private const float _threshold = 0.01f;

	//Player Movement Variables (Used to be Protected Static Variables in PlayerBaseState)
	[HideInInspector] public float speed;
	[HideInInspector] public float animationBlend;
	[HideInInspector] public float animationBlendVelX;
	[HideInInspector] public float animationBlendVelZ;
	[HideInInspector] public float targetRotation = 0.0f;
	[HideInInspector] public float rotationVelocity;
	[HideInInspector] public float verticalVelocity;
	[HideInInspector] public float terminalVelocity = 53.0f;

	//Cache
	PlayerInput playerInput;
	public AimController aimController { get; private set; }
	public PlayerAnimator playerAnimator { get; private set; }
	public CharacterController controller { get; private set; }
	public Camera mainCamera { get; private set; }

	public SuperSpeedController superSpeedController { get; private set; }

	//States
	public PlayerDefaultState playerDefaultState { get; private set; }
	public PlayerEnterCoverState playerEnterCoverState { get; private set; }
	public PlayerCoverState playerCoverState { get; private set; }
	public PlayerExitCoverState playerExitCoverState { get; private set; }
	public PlayerWeaponEquipState playerWeaponEquipState { get; private set; }
	public PlayerWeaponDropState playerWeaponDropState { get; private set; }
	public PlayerHolsterState playerHolsterState { get; private set; }

	public PlayerCombatState playerCombatState { get; private set; }

	//Events
	public Action InteractWithObject;


	private void Awake()
    {
		mainCamera = Camera.main;
		controller = GetComponent<CharacterController>();
		aimController = GetComponent<AimController>();
		playerAnimator = GetComponent<PlayerAnimator>();
		playerInput = ControlsManager.Instance.GetPlayerInput();
		superSpeedController = GetComponent<SuperSpeedController>();

		//Set States
		playerDefaultState = new PlayerDefaultState(this);
		playerEnterCoverState = new PlayerEnterCoverState(this);
		playerCoverState = new PlayerCoverState(this);
		playerExitCoverState = new PlayerExitCoverState(this);
		playerWeaponEquipState = new PlayerWeaponEquipState(this);
		playerWeaponDropState = new PlayerWeaponDropState(this);
		playerHolsterState = new PlayerHolsterState(this);
		playerCombatState = new PlayerCombatState(this);

		//Intialize States (This is when they set Banned Transitions)
		IntializeStates();

		//Set Character Controller Default Values
		SetIntialControllerConfig();
	}

	private void OnEnable()
	{
		playerInput = ControlsManager.Instance.GetPlayerInput();
		playerInput.onActionTriggered += OnMove;
		playerInput.onActionTriggered += OnLook;
		playerInput.onActionTriggered += OnSprint;
		playerInput.onActionTriggered += OnAim;
		playerInput.onActionTriggered += OnInteract;
		playerInput.onActionTriggered += OnDrop;
		playerInput.onActionTriggered += OnQuickSwitch;
		playerInput.onActionTriggered += TakeCover;
		playerInput.onActionTriggered += ExitCover;
	}

	private void Start()
    {
		SwitchState(playerDefaultState);
    }

	private void IntializeStates()
    {
		playerDefaultState.SetBannedTransitions();
		playerEnterCoverState.SetBannedTransitions();
		playerCoverState.SetBannedTransitions();
		playerExitCoverState.SetBannedTransitions();
		playerWeaponEquipState.SetBannedTransitions();
		playerWeaponDropState.SetBannedTransitions();
		playerHolsterState.SetBannedTransitions();
		playerCombatState.SetBannedTransitions();
    }


    public void CameraRotation(float aimSensitivityX, float aimSensitivityY, float leftClamp, float rightClamp)
    {
		// if there is an input and camera position is not fixed
		if (InputLookValue.sqrMagnitude >= _threshold && !LockCameraPosition)
		{
			Vector2 lookValue = InputLookValue;
			bool isCurrentDeviceMouse = ControlsManager.Instance.IsCurrentControlScheme("KeyboardMouse");
			float deltaTimeMultiplier = isCurrentDeviceMouse ? 1.0f : Time.unscaledDeltaTime;

			if (ControlsManager.Instance.invertCamX)
			{
				cinemachineTargetYaw += -lookValue.x * deltaTimeMultiplier * GameManager.Instance.XSensitivity * aimSensitivityX;
			}
			else
			{
				cinemachineTargetYaw += lookValue.x * deltaTimeMultiplier * GameManager.Instance.XSensitivity * aimSensitivityX;
			}


			if (ControlsManager.Instance.invertCamY)
			{
				cinemachineTargetPitch += -lookValue.y * deltaTimeMultiplier * GameManager.Instance.YSensitivity * aimSensitivityY;
			}
			else
			{
				cinemachineTargetPitch += lookValue.y * deltaTimeMultiplier * GameManager.Instance.YSensitivity * aimSensitivityY;
			}

		}

		// clamp our rotations so our values are limited 360 degrees
		cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, leftClamp, rightClamp);
		cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

		// Cinemachine will follow this target
		CinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch + CameraAngleOverride, cinemachineTargetYaw, 0.0f);
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	private void SetIntialControllerConfig()
    {
		defaultControllerCenter = controller.center;
		defaultControllerRadius = controller.radius;
		defaultControllerHeight = controller.height;
	}


	private void OnDisable()
	{
		playerInput.onActionTriggered -= OnMove;
		playerInput.onActionTriggered -= OnLook;
		playerInput.onActionTriggered -= OnSprint;
		playerInput.onActionTriggered -= OnAim;
		playerInput.onActionTriggered -= OnInteract;
		playerInput.onActionTriggered -= OnDrop;
		playerInput.onActionTriggered -= OnQuickSwitch;
		playerInput.onActionTriggered -= TakeCover;
		playerInput.onActionTriggered -= ExitCover;
	}

	//Auto Move Animation Events
	private void BeginAutoMove()
	{
		autoMoverActive = true;
	}

	private void EndAutoMove()
	{
		autoMoverActive = false;
		autoMoverDestination = Vector3.zero;
	}

	//Getters
	public bool IsCurrentState(State state)
    {
		return currentState == state;
    }

	//Inputs
	private void OnMove(InputAction.CallbackContext context)
	{
		if (context.action.name != "Move") return;

		InputMoveValue = context.ReadValue<Vector2>();
	}

	private void OnLook(InputAction.CallbackContext context)
	{
		if (context.action.name != "Look") return;

		InputLookValue = context.ReadValue<Vector2>();

	}


	private void OnSprint(InputAction.CallbackContext context)
	{
		if (context.action.name != "Sprint") return;

		if (context.performed)
		{
			isSpeedBoostActivated = true;
		}
		else if (context.canceled)
		{
			isSpeedBoostActivated = false;
		}
	}

	private void OnAim(InputAction.CallbackContext context)
	{
		if (context.action.name != "Aim") return;

		if (context.performed)
		{
			isAimTriggered = true;
		}
		else if (context.canceled)
		{
			isAimTriggered = false;
		}
	}



	//Object interaction Input
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
			InventoryManager.Instance.IsGroundEquip(false);
			SwitchState(playerWeaponDropState);
		}
	}

	private void OnQuickSwitch(InputAction.CallbackContext context)
	{
		if (context.action.name != "QuickSwitch") return;

		if (context.performed && InventoryManager.Instance.CanQuickSwitch())
		{
			InventoryManager.Instance.IsGroundEquip(false);
			SwitchState(playerHolsterState);
		}
	}

	//Cover Input Methods
	private void TakeCover(InputAction.CallbackContext context)
	{
		if (context.action.name != "TakeCover") return;

		if (context.performed)
		{
			SwitchState(playerEnterCoverState);
		}
	}

	private void ExitCover(InputAction.CallbackContext context)
	{
		if (context.action.name != "ExitCover") return;

		if (context.performed)
		{
			SwitchState(playerExitCoverState);
		}
	}
}
