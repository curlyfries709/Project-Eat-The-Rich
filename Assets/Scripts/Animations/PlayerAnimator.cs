using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] float throwGunPower;
    [SerializeField] float animationEventCooldown = 0.3f;
    [SerializeField] float activeShootLayerWeight = 0.5f;

    bool animationEventTriggered = false;

    //Caches
    Animator animator;
    RuntimeAnimatorController defaultAnimatorController;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        defaultAnimatorController = animator.runtimeAnimatorController;
    }

    private void Start()
    {
        InventoryManager.Instance.SetThrowPower(throwGunPower);
    }

    private void Update()
    {
        animator.SetBool("Armed", InventoryManager.Instance.GetCurrentEquippedGun());
    }

    public void TriggerShootAnimation()
    {
        SetLayerWeight(2, activeShootLayerWeight);
        animator.SetTrigger("Shoot");
    }


    //Handy Methods
    public void SetBool(int boolId, bool value)
    {
        animator.SetBool(boolId, value);
    }

    public void SetFloat(int floatId, float value )
    {
        animator.SetFloat(floatId, value);
    }

    public void SetLayerWeight(int layerIndex, float layerWeight)
    {
        animator.SetLayerWeight(layerIndex, layerWeight);
    }

    public void SetTrigger(int triggerIndex)
    {
        animator.SetTrigger(triggerIndex);
    }

    public float GetLayerWeight(int layerIndex)
    {
        return animator.GetLayerWeight(layerIndex);
    }

    public bool IsCurrentAnimStateActive(string stateTag)
    {
        //return (animator.GetCurrentAnimatorStateInfo(GetCurrentDominantLayer()).IsTag(stateTag) && !animator.IsInTransition(GetCurrentDominantLayer()))|| animator.GetNextAnimatorStateInfo(GetCurrentDominantLayer()).IsTag(stateTag);
        return animator.GetCurrentAnimatorStateInfo(GetCurrentDominantLayer()).IsTag(stateTag) || animator.GetNextAnimatorStateInfo(GetCurrentDominantLayer()).IsTag(stateTag);

    }

    public int GetCurrentDominantLayer()
    {
        for (int i = animator.layerCount - 1; i >= 0; i--)
        {
            //We Loop Backwards & First Full weight layer we find is the dominant one.
            if (animator.GetLayerWeight(i) >= 1)
            {
                return i;
            }
        }

        return 0;
    }

    
    public void UpdateCurrentAnimatorOverride(Gun gun)
    {
        if (gun)
        {
            AnimatorOverrideController gunOverrideController = gun.GetGunConfig().animatorOverrideController;
            animator.runtimeAnimatorController = gunOverrideController;
        }
        else
        {
            
            animator.runtimeAnimatorController = new AnimatorOverrideController(defaultAnimatorController);
        }
    }

    public void StartGroundEquipRoutine()
    {
        StartCoroutine(GroundEquipRoutine());
    }

    //Coroutines
    IEnumerator GroundEquipRoutine()
    {
        yield return null;
        animator.SetBool("GroundEquip", false);
    }

    IEnumerator AnimationEventCoolDown()
    {
        animationEventTriggered = true;
        yield return new WaitForSeconds(animationEventCooldown);
        animationEventTriggered = false;
    }

    //Animation Events
    public void DropGun()
    {
        if (!animationEventTriggered)
        {
            animator.ResetTrigger("StorageEquip");

            //Drop: Throw Current Gun. Set New Current Gun.
            InventoryManager.Instance.DropGun(InventoryManager.Instance.GetCurrentEquippedGun().transform.GetSiblingIndex());

            if (InventoryManager.Instance.ShouldEquipGunAfterDropping())
            {
                if (InventoryManager.Instance.IsGroundEquip())
                {
                    animator.SetBool("GroundEquip", true);
                }
                else
                {
                    animator.SetTrigger("StorageEquip");
                }

            }

            StartCoroutine(AnimationEventCoolDown());
        }
    }

    public void HolsterGun()
    {
        //Holster: Hide Old Gun. Set New Current Gun.
        InventoryManager.Instance.HolsterGun();

        if (!InventoryManager.Instance.IsGroundEquip())
        {
            Debug.Log("Trigger Storage Equip After Holster");
            animator.SetTrigger("StorageEquip");
        }
    }

    public void ActivateCurrentGun()
    {
        //Storage Equip: Activate Current Gun.
        InventoryManager.Instance.ActivateCurrentGun();
    }


    /* private void LerpLayerWeight()
     {
         if (playerController.IsAiming())
         {
             currentAimLayerWeight = Mathf.Min(currentAimLayerWeight + (Time.deltaTime / aimLayerTransistionTime), 1);

             animator.SetLayerWeight(1, currentAimLayerWeight);

         }
         else
         {
             currentAimLayerWeight = Mathf.Max(currentAimLayerWeight - (Time.deltaTime / aimLayerTransistionTime), 0);

             animator.SetLayerWeight(1, currentAimLayerWeight);

         }

     }*/
}
