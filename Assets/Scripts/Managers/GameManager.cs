using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("Inventory Manager is NULL");
            }

            return instance;
        }
    }
    [Header("Default Camera Sensitivity")]
    public float XSensitivity = 1;
    public float YSensitivity = 1;
    [Header("Default Aim Sensitivity")]
    public float aimXSensitivity = 0.5f;
    public float aimYSensitivity = 0.5f;

    private void Awake()
    {
        instance = this;
    }
}
