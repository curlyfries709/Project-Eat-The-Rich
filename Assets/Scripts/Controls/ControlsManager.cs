using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsManager : MonoBehaviour
{
    private static ControlsManager instance;
    public static ControlsManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("Controls Manager is NULL");
            }

            return instance;
        }
    }

    [Header("Invert Cam Controls")]
    public bool invertCamX = false;
    public bool invertCamY = false;

    //Variables
    string previousActionMap;

    //Caches
    PlayerInput playerInput;

    private void Awake()
    {
        instance = this;
        playerInput = GetComponent<PlayerInput>();
        previousActionMap = playerInput.currentActionMap.name;
    }

    private void OnApplicationFocus(bool focus)
    {
        Cursor.lockState = focus ? CursorLockMode.Locked : CursorLockMode.None;
    }


    //Control Switching
    public void DisableControls()
    {
        if (!IsCurrentActionMap("NoControls"))
        {
            previousActionMap = playerInput.currentActionMap.name;
            playerInput.SwitchCurrentActionMap("NoControls");
        }
    }

    public void EnableCurrentControls()
    {
        if (IsCurrentActionMap("NoControls"))
        {
            print("Re Enabling Controls");
            playerInput.SwitchCurrentActionMap(previousActionMap);
        }

    }

    public void SwitchControls(string newActionMap)
    {
        if (newActionMap != playerInput.currentActionMap.name)
        {
            previousActionMap = playerInput.currentActionMap.name;
            playerInput.SwitchCurrentActionMap(newActionMap);
        }
    }

    //---Questions---
    public bool IsCurrentControlScheme(string controlSchemeName)
    {
        return controlSchemeName == playerInput.currentControlScheme;
    }

    public bool IsCurrentActionMap(string actionMapName)
    {
        return playerInput.currentActionMap.name == actionMapName;
    }
    
    //---Getters----
 #region
    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }

    public string GetCurrentControlScheme()
    {
        return playerInput.currentControlScheme;
    }

#endregion
}
