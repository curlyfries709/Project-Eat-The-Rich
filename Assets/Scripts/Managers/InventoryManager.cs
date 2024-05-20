using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;
    public static InventoryManager Instance
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

    //Serialized fields
    [Header("Gun Inventory Configs")]
    [SerializeField] int maxGunInventoryCapacity = 2;
    [SerializeField] float dropGunOffset = 1;
    [Header("Transforms")]
    [SerializeField] Transform gunInventoryHeader; //Child of Player's Right hand
    [SerializeField] Transform playerTransform;


    //Gun Inventory Variables
    Gun currentEquippedGun = null;
    Gun previousEquippedGun = null;
    float throwPower;

    //Gun Inventory bools
    bool groundEquip = false;
    bool equipNewGun = false;

    //Newly Equipped Gun Data
    Gun newGunClass;
    Rigidbody newGunRB;
    GameObject newGunRadius;
    Transform newGunEquipTransform;
    Collider newGunCollider;


    private void Awake()
    {
        instance = this;
    }

    public void PickUpNewGun(Gun gunClass, Rigidbody gunRB, GameObject gunRadius, Collider gunCollider, Transform gunEquipTransform)
    {
        newGunClass = gunClass;
        newGunRB = gunRB;
        newGunRadius = gunRadius;
        newGunCollider = gunCollider;
        newGunEquipTransform = gunEquipTransform;
    }

    public void EquipGun()
    {
        Transform gunTransform = newGunClass.transform;
        Debug.Log("Equpping Gun Called: " + newGunClass.name);

        //Set Gun Rigidbody to Kinematic
        newGunRB.isKinematic = true;
        //Deactivate Interaction Radius & Collider
        newGunRadius.SetActive(false);
        newGunCollider.enabled = false;
        //Child Gun Gameobject to Player hand
        gunTransform.parent = gunInventoryHeader;
        //Set Gun Position from Equip Transform
        gunTransform.localPosition = newGunEquipTransform.localPosition;
        gunTransform.localRotation = newGunEquipTransform.localRotation;

        //Update Current Equipped Gun.
        currentEquippedGun = newGunClass;
    }

    public void DropGun(int gunInventoryIndex)
    {
        if (gunInventoryHeader.childCount > 0)
        {
            Transform gunTransform = gunInventoryHeader.GetChild(gunInventoryIndex);
            Gun gunClass = gunTransform.GetComponent<Gun>();
            bool isDroppingPreviousGun = false;

            if(previousEquippedGun && gunInventoryIndex == previousEquippedGun.transform.GetSiblingIndex() && !gunTransform.gameObject.activeInHierarchy)
            {
                isDroppingPreviousGun = true;
            }

            gunTransform.parent = null;
            gunClass.GetModelCollider().enabled = true;
            gunClass.GetRigidbody().isKinematic = false;
            gunClass.Dropped();//Interaction Radius Re-enabled in Gun Class

            if (gunTransform.gameObject.activeInHierarchy) //Means it's current Equipped Gun
            {
                currentEquippedGun = null;

                if(ShouldEquipGunAfterDropping() && !groundEquip)
                {
                    EquipPreviousGun();
                }
                else if (!ShouldEquipGunAfterDropping())
                {
                    previousEquippedGun = null; //Reset Previous Equipped Gun Because Player's Unarmed
                }
            }
            else if(isDroppingPreviousGun && gunInventoryHeader.childCount > 1)
            {
                UpdatePrevGun();//Find first Gun in storage to update previously Equipped Gun.
            }
            //Activate & Drop in Environment
            gunTransform.gameObject.SetActive(true);
            gunClass.GetRigidbody().AddForce(playerTransform.right.normalized * throwPower, ForceMode.VelocityChange);
        }
    }

    public void EquipGunFromStorage(int gunInventoryIndex)
    {
        if (currentEquippedGun)
        {
            StoreCurrentGun();
        }

        Transform gunTransform = gunInventoryHeader.GetChild(gunInventoryIndex);
        currentEquippedGun = gunTransform.GetComponent<Gun>();

        //gunTransform.gameObject.SetActive(true);
    }

    public void EquipPreviousGun()
    {
        if (!previousEquippedGun)
        {
            UpdatePrevGun();
        }

        EquipGunFromStorage(previousEquippedGun.transform.GetSiblingIndex());
    }

    private void StoreCurrentGun()
    {
        if (currentEquippedGun)
        {
            currentEquippedGun.gameObject.SetActive(false);
            previousEquippedGun = currentEquippedGun;
        }
    }

    private void UpdatePrevGun()
    {
        foreach (Transform gun in gunInventoryHeader)
        {
            if (!gun.gameObject.activeInHierarchy)
            {
                previousEquippedGun = gun.GetComponent<Gun>();
                break;
            }
        }
    }

    //Animation Methods

    public bool IsGroundEquip()
    {
        return groundEquip;
    }
    public void HolsterGun()
    {
        if(currentEquippedGun)
            currentEquippedGun.gameObject.SetActive(false);
    }

    public void ActivateCurrentGun()
    {
        if(currentEquippedGun)
            currentEquippedGun.gameObject.SetActive(true);
    }

    //Old Methods
    private void SwitchGun(Gun newGun)
    {
        if (gunInventoryHeader.childCount > 1)
        {
            EquipGunFromStorage(newGun.transform.GetSiblingIndex());
        }
    }

    public void QuickSwitch()
    {
        SwitchGun(previousEquippedGun);
    }

    //Getters
    public bool IsGunInvetoryFull()
    {
        return gunInventoryHeader.childCount == maxGunInventoryCapacity;
    }

    public bool CanHolster()
    {
        return currentEquippedGun && currentEquippedGun.gameObject.activeInHierarchy;
    }

    public Gun GetCurrentEquippedGun()
    {
        return currentEquippedGun;
    }

    public bool CanQuickSwitch()
    {
        return gunInventoryHeader.childCount > 1;
    }

    public bool ShouldEquipGunAfterDropping()
    {
        return equipNewGun || gunInventoryHeader.childCount > 0;
    }

    //Setter
    public void SetEquipNewGun(bool value)
    {
        equipNewGun = value;
    }

    public void SetThrowPower(float power)
    {
        throwPower = power;
    }

    public void IsGroundEquip(bool isGroundEquip)
    {
        groundEquip = isGroundEquip;
    }
}
