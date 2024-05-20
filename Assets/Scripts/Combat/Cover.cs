using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Cover : MonoBehaviour
{
    [Header("Raycast Lengths")]
    [SerializeField] float maxDistanceFromCover = 5f;
    [SerializeField] float horizontalCoverDetectorLength = 1f;
    [Header("Raycast Points")]
    [SerializeField] Transform highCoverDetectionTransform;
    [SerializeField] Transform leftCoverDetectionTransform;
    [SerializeField] Transform rightCoverDetectionTransform;

    //Bools
    bool inCover = false;
    bool inHighCover = false;
    bool canAim = false;


    //Cache 
    PlayerInput playerInput;
    LayerMask coverLayerMask;
    ICharacterMover characterMover;
    Vector3 coverHitPoint;
    Vector3 coverSurfaceDirection;
   

    private void Awake()
    {
        coverLayerMask = LayerMask.GetMask(LayerMask.LayerToName(6));
        characterMover = GetComponent<ICharacterMover>();
    }

    private void OnEnable()
    {
        if (gameObject.CompareTag("Player"))
        {
            playerInput = ControlsManager.Instance.GetPlayerInput();
            playerInput.onActionTriggered += TakeCover;
            playerInput.onActionTriggered += ExitCover;
        }
        
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, transform.forward.normalized * maxDistanceFromCover, Color.red);

        if (inCover)
        {
            SetCoverType();
            InCoverMovementRestrictor();
        }
    }


    private void MoveCharacterToCover()
    {
        inCover = true;
        SetCharacterMoverCoverDirections(coverSurfaceDirection, Vector3.zero);
        characterMover.BeginMoveToCover(coverHitPoint);
    }

    private void InCoverMovementRestrictor()
    {
        bool didRightCoverDetectorHit = Physics.Raycast(rightCoverDetectionTransform.position, rightCoverDetectionTransform.forward, horizontalCoverDetectorLength, coverLayerMask);
        bool didLeftCoverDetectorHit = Physics.Raycast(leftCoverDetectionTransform.position, leftCoverDetectionTransform.forward, horizontalCoverDetectorLength, coverLayerMask);

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
                SetCharacterMoverCoverDirections(coverSurfaceDirection, -coverSurfaceDirection);
            }
            else
            {
                SetCharacterMoverCoverDirections(coverSurfaceDirection, coverSurfaceDirection);
            } 
        }
        else
        {
            if (inHighCover)
            {
                canAim = false;
            }

            SetCharacterMoverCoverDirections(coverSurfaceDirection, Vector3.zero);
        }
    }
    //---Player Input Methods
   



    //---Setters---
    private void SetCoverType()
    {
        float rayLength = inCover ? horizontalCoverDetectorLength : maxDistanceFromCover;

        Debug.DrawRay(highCoverDetectionTransform.position, highCoverDetectionTransform.forward.normalized * rayLength, Color.green);
        if (Physics.Raycast(highCoverDetectionTransform.position, highCoverDetectionTransform.forward, rayLength, coverLayerMask))
        {
            inHighCover = true;
        }
        else
        {
            canAim = true;
            inHighCover = false;
        }

        characterMover.inHighCover = inHighCover;
    }

    private void SetCharacterMoverCoverDirections(Vector3 moveDirection, Vector3 directionToProhibit)
    {
        characterMover.inCoverMoveDirection = moveDirection;
        characterMover.inCoverProhibitedDirection = directionToProhibit;
    }

    //---Getters---
    private bool IsNearCover()
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, maxDistanceFromCover, coverLayerMask))
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

    private Vector3 GetCoverSurfaceDirection(Vector3 hitNormal)
    {
        return Vector3.Cross(hitNormal, Vector3.up).normalized;
    }


    private void OnDisable()
    {
        if (gameObject.CompareTag("Player"))
        {
            playerInput.onActionTriggered -= TakeCover;
            playerInput.onActionTriggered -= ExitCover;
        }
        
    }

    private void TakeCover(InputAction.CallbackContext context)
    {
        if (context.action.name != "TakeCover") return;

        if (context.performed)
        {
            if (IsNearCover())
            {
                SetCoverType();
                MoveCharacterToCover();
                //Call Combat Action Triggered Event.
            }
        }
    }

    private void ExitCover(InputAction.CallbackContext context)
    {
        if (context.action.name != "ExitCover") return;

        if (context.performed && inCover)
        {
            inCover = false;
            characterMover.ExitCover();

        }
    }
}
