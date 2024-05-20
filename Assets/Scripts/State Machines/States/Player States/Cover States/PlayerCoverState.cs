using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerCoverState : PlayerBaseState
{
    //Cover Variables
    protected LayerMask coverLayerMask;
    protected static Vector3 coverHitPoint;
    protected static Vector3 coverSurfaceDirection;

    //Bools
    protected bool inHighCover = false;
    protected bool canAim = false;
    protected bool atRightCorner = false;

    //Vectors
    protected static Vector3 inCoverMoveDirection;
    protected static Vector3 inCoverProhibitedDirection;
    protected Vector3 shootFromHighCoverPos;
    protected Vector3 cornerReturnPos;


    public override List<State> bannedStateTransitions { get; set; }

    public PlayerCoverState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
        coverLayerMask = LayerMask.GetMask(LayerMask.LayerToName(6));
    }

    public override void SetBannedTransitions()
    {
        bannedStateTransitions = new List<State>();
        bannedStateTransitions.Add(stateMachine.playerEnterCoverState);
    }

    public override void EnterState()
    {
        //Re-enable Player Input.
        //ControlsManager.Instance.EnableCurrentControls();
    }

    public override void UpdateState()
    {
        Aim();

        GeneralMovementMethods();

        if (!stateMachine.aimController.IsAimingFromHighCover())
        {
            InCoverMove();
        }
        
        SetCoverType(stateMachine.horizontalCoverDetectorLength);
        InCoverMovementRestrictor();
    }

    public override void LateUpdateState()
    {    
       if (stateMachine.aimController.enabled)
        {
            float leftClamp;
            float rightClamp;

            if (stateMachine.aimController.IsAimingFromHighCover())
            {
                leftClamp = -stateMachine.aimController.GetHighCoverAngleChange() + transform.rotation.eulerAngles.y;
                rightClamp = stateMachine.aimController.GetHighCoverAngleChange() + transform.rotation.eulerAngles.y;
            }
            else
            {
                leftClamp = -stateMachine.aimController.GetLowCoverAngleChange() + transform.rotation.eulerAngles.y;
                rightClamp = stateMachine.aimController.GetLowCoverAngleChange() + transform.rotation.eulerAngles.y;
            }

            stateMachine.CameraRotation(GameManager.Instance.aimXSensitivity, GameManager.Instance.aimYSensitivity, leftClamp, rightClamp);
        }
        else
        {
            stateMachine.CameraRotation(1, 1, float.MinValue, float.MaxValue);
        }
    }

    public override void ExitState()
    {
        
    }

    public override bool CanEnterState()
    {
        return true;
    }

    protected void GeneralMovementMethods()
    {
        JumpAndGravity();
        GroundedCheck();
    }

    private void Aim()
    {
        bool triggerAim = CanSwitchToAim() && canAim;

        float aimWeight = triggerAim ? 1 : 0;

        stateMachine.aimController.ShootFromHighCoverSetup(inCoverProhibitedDirection, inHighCover, atRightCorner);
        stateMachine.aimController.SetRigWeight(aimWeight);
        stateMachine.aimController.enabled = triggerAim;

        //Trigger Anim
        playerAnimator.SetBool(animIDAiming, triggerAim);

    }

    private void InCoverMovementRestrictor()
    {
        bool didRightCoverDetectorHit = Physics.Raycast(stateMachine.rightCoverDetectionTransform.position, stateMachine.rightCoverDetectionTransform.forward, stateMachine.horizontalCoverDetectorLength, coverLayerMask);
        bool didLeftCoverDetectorHit = Physics.Raycast(stateMachine.leftCoverDetectionTransform.position, stateMachine.leftCoverDetectionTransform.forward, stateMachine.horizontalCoverDetectorLength, coverLayerMask);

        if (!didLeftCoverDetectorHit || !didRightCoverDetectorHit)
        {
            //Means we're at the Cover's corner. 
            if (inHighCover)
            {
                canAim = true;
            }

            //Set Move Directions
            if (!didLeftCoverDetectorHit)
            {
                SetMoveDirections(coverSurfaceDirection, -coverSurfaceDirection);
                atRightCorner = false;
            }
            else
            {
                SetMoveDirections(coverSurfaceDirection, coverSurfaceDirection);
                atRightCorner = true;
            }
        }
        else
        {
            if (inHighCover)
            {
                canAim = false;
            }

            SetMoveDirections(coverSurfaceDirection, Vector3.zero);
        }
    }

    private void InCoverMove()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = stateMachine.defaultMoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // if there is no left or right input, set the target speed to 0
        if (stateMachine.InputMoveValue.x == 0) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(stateMachine.controller.velocity.x, 0.0f, stateMachine.controller.velocity.z).magnitude;

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

        // normalise input direction
        //Vector3 inputDirection = new Vector3(moveValue.x, 0.0f, moveValue.y).normalized;

        //Update Rotation
        Vector3 perpDirection = Vector3.Cross(inCoverMoveDirection, Vector3.up);
        //Quaternion lookRotation = Quaternion.LookRotation(perpDirection);
        //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, RotationSmoothTime * Time.deltaTime);
        transform.forward = perpDirection;

        Vector3 moveDirection = inCoverMoveDirection.normalized * stateMachine.InputMoveValue.x;

        // move the player
        if (moveDirection != inCoverProhibitedDirection.normalized)
        {
            stateMachine.controller.Move(moveDirection * (stateMachine.speed * Time.deltaTime) + new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else
        {
            targetSpeed = 0f;
        }

        stateMachine.animationBlend = Mathf.Lerp(stateMachine.animationBlend, targetSpeed * stateMachine.InputMoveValue.x, Time.deltaTime * stateMachine.SpeedChangeRate);

        //Update Character Controller
        if (inHighCover)
        {
            SetCharacterControllerConfigs(stateMachine.defaultControllerCenter, stateMachine.defaultControllerRadius, stateMachine.defaultControllerHeight);
        }
        else
        {
            SetCharacterControllerConfigs(stateMachine.crouchControllerCenter, stateMachine.crouchControllerRadius, stateMachine.crouchControllerHeight);
        }

        playerAnimator.SetFloat(animIDSpeed, stateMachine.animationBlend);
        playerAnimator.SetFloat(animIDMotionSpeed, 1f);
        playerAnimator.SetBool(animIDInHighCover, inHighCover);
    }


    //Setters
    protected void SetCoverType(float rayLength)
    {
        //RayLength Shorter if in Cover
        //(OLD CODE) float rayLength = inCover ? horizontalCoverDetectorLength : maxDistanceFromCover;

        Debug.DrawRay(stateMachine.highCoverDetectionTransform.position, stateMachine.highCoverDetectionTransform.forward.normalized * rayLength, Color.green);
        if (Physics.Raycast(stateMachine.highCoverDetectionTransform.position, stateMachine.highCoverDetectionTransform.forward, rayLength, coverLayerMask))
        {
            inHighCover = true;
        }
        else
        {
            canAim = true;

            if (stateMachine.aimController.IsAimingFromHighCover())
            {
                //Means We're Aiming From High Cover
                inHighCover = true;
            }
            else
            {
                inHighCover = false;
            }
        }
    }

    protected void SetMoveDirections(Vector3 moveDirection, Vector3 directionToProhibit)
    {
        inCoverMoveDirection = moveDirection;
        inCoverProhibitedDirection = directionToProhibit;
    }

    //Getters
    protected bool IsNearCover()
    {
        RaycastHit hitInfo;

        Debug.DrawRay(transform.position, transform.forward.normalized * stateMachine.maxDistanceFromCover, Color.green, 1000f);

        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, stateMachine.maxDistanceFromCover, coverLayerMask))
        {
            coverHitPoint = hitInfo.point;
            coverSurfaceDirection = GetCoverSurfaceDirection(hitInfo.normal);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected Vector3 GetCoverSurfaceDirection(Vector3 hitNormal)
    {
        return Vector3.Cross(hitNormal, Vector3.up).normalized;
    }


}
