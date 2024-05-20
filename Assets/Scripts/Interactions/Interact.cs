using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
{
    [SerializeField] protected Canvas interactCanvas;
    protected bool canHandleInteracion = false;

    //List
    private static List<Transform> currentActiveRadii = new List<Transform>();

    //Caches
    TextMeshProUGUI actionKey;
    protected PlayerStateMachine playerStateMachine;
    //PlayerStateMachine playerStateMachine;

    private void Awake()
    {
        actionKey = interactCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (!playerStateMachine)
        {
            playerStateMachine = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateMachine>();
        }

        ControlsManager.Instance.GetPlayerInput().onControlsChanged += UpdateActionKey;
        playerStateMachine.InteractWithObject += HandleInteraction;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!currentActiveRadii.Contains(transform))
            {
                currentActiveRadii.Add(transform);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(currentActiveRadii.Count <= 1)
            {
                AllowInteraction(true);
            }
            else
            {
                if (IsClosestInteractable())
                {
                    AllowInteraction(true);
                }
                else
                {
                    AllowInteraction(false);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ExitInteraction();
        }
    }

    protected virtual void HandleInteraction()
    {
        //Overriden by children classes
    }

    private void AllowInteraction(bool interactionallowed)
    {
        canHandleInteracion = interactionallowed;
        interactCanvas.gameObject.SetActive(interactionallowed);
    }

    protected void ExitInteraction()
    {
        AllowInteraction(false);
        currentActiveRadii.Remove(transform);
    }

    private bool IsClosestInteractable()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestTransform = null;

        foreach (Transform radius in currentActiveRadii)
        {
            float calculatedDistance = Vector3.Distance(radius.position, playerStateMachine.transform.position);
            if (calculatedDistance < closestDistance)
            {
                closestDistance = calculatedDistance;
                closestTransform = radius;
            }
        }

        if(closestTransform == transform)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    private void UpdateActionKey(PlayerInput playerInput)
    {
        switch (playerInput.currentControlScheme)
        {
            case "PlaystationController":
                actionKey.text = "R2";
                break;
            case "XboxController":
                actionKey.text = "RT";
                break;
            default:
                actionKey.text = "E";
                break;
        }
    }


    private void OnDisable()
    {
        ControlsManager.Instance.GetPlayerInput().onControlsChanged -= UpdateActionKey;
        playerStateMachine.InteractWithObject -= HandleInteraction;
    }

}
