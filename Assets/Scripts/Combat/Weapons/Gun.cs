using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Gun : Interact
{
    [SerializeField] GunConfig gunConfig;
    [Header("Game Objects")]
    public GameObject projectilePrefab;
    [SerializeField] Collider modelCollider;
    [Header("Muzzle Flash")]
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] float muzzleFlashTime = 0.18f;
    [Header("Recoil Impulses")]
    [SerializeField] CinemachineImpulseSource[] recoilImpulseSources = new CinemachineImpulseSource[3];
    [Header("Transforms")]
    [SerializeField] Transform equipTransform;
    [SerializeField] Transform shootPointTransform;
    [Header("Color")]
    [SerializeField] Color specialWeaponColor;

    //Variables
    int currentAmmoCount;
    int[] recoilXAmpMultipliers = { -1, 1 };
    float recoilXAmp;

    //Caches
    TextMeshProUGUI gunNameText;
    TextMeshProUGUI gunNameType;
    Rigidbody rigidbody;
    GameObject interactionRadius;

    WaitForSeconds timeBetweenShots;
    WaitForSeconds timeBetweenMuzzleFlash;

    //bools
    bool equipped = false;
    bool canFire = true;

    private void Awake()
    {
        gunNameText = interactCanvas.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        gunNameType = interactCanvas.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        rigidbody = GetComponent<Rigidbody>();
        interactionRadius = interactCanvas.transform.parent.gameObject;

        currentAmmoCount = gunConfig.baseAmmoCapacity;
        timeBetweenShots = new WaitForSeconds(gunConfig.baseFiringRate);
        timeBetweenMuzzleFlash = new WaitForSeconds(muzzleFlashTime);
        recoilXAmp = recoilImpulseSources[0].m_ImpulseDefinition.m_AmplitudeGain;

    }

    private void Start()
    {
        muzzleFlash.SetActive(false);
        UpdateGunUIData();
    }

    private void UpdateGunUIData()
    {
        gunNameText.text = gunConfig.gunName;
        gunNameType.text = gunConfig.gunType.ToString();

        if (gunConfig.gunType == GunConfig.GunType.Special)
        {
            gunNameType.color = specialWeaponColor;
        }
    }

    protected override void HandleInteraction()
    {
        if (canHandleInteracion)
        {
            Equip();
        }
    }

    private void Equip()
    {
        if (!equipped)
        {
            equipped = true;
            ExitInteraction();
            InventoryManager.Instance.IsGroundEquip(true);
            InventoryManager.Instance.PickUpNewGun(this, rigidbody, interactionRadius, modelCollider, equipTransform);
            playerStateMachine.SwitchState(playerStateMachine.playerWeaponEquipState);
        }
    }

    //Fire Methods
    public void Shoot(Vector3 projectileDestination)
    {
        if (canFire && currentAmmoCount > 0)
        {
            //Trigger Animation
            playerStateMachine.playerAnimator.TriggerShootAnimation();

            //Spawn Projectile
            GameObject projectileObj = Instantiate(projectilePrefab, shootPointTransform.position, shootPointTransform.rotation);
            Projectile projectileClass = projectileObj.GetComponent<Projectile>();
            projectileClass.Setup(shootPointTransform.position, projectileDestination, gunConfig.baseDamage, gunConfig.baseRange);

            //Recoil
            GeneratRecoil();

            //Reduce Ammo
            currentAmmoCount = currentAmmoCount - 1;

            //Start Corotines
            StartCoroutine(FireRateRoutine());

            if(!muzzleFlash.activeInHierarchy)
                StartCoroutine(MuzzleFlashRoutine());
        }
    }

    private void GeneratRecoil()
    {
        //Randomise X impulse
        recoilImpulseSources[0].m_ImpulseDefinition.m_AmplitudeGain = recoilXAmp * recoilXAmpMultipliers[Random.Range(0, recoilXAmpMultipliers.Length)];

        foreach (CinemachineImpulseSource impulse in recoilImpulseSources)
        {
            impulse.GenerateImpulse();
        }
    }

    //Fire Coroutines
    IEnumerator FireRateRoutine()
    {
        canFire = false;
        yield return timeBetweenShots;
        canFire = true;
    }
    IEnumerator MuzzleFlashRoutine()
    {
        muzzleFlash.SetActive(true);
        yield return timeBetweenMuzzleFlash;
        muzzleFlash.SetActive(false);
    }

    //Getter
    public GunConfig GetGunConfig()
    {
        return gunConfig;
    }

    public int GetAmmoCount()
    {
        return currentAmmoCount;
    }

    public Transform GetShootPoint()
    {
        return shootPointTransform;
    }
    
    public Collider GetModelCollider()
    {
        return modelCollider;
    }

    public Rigidbody GetRigidbody()
    {
        return rigidbody;
    }

    public void Dropped()
    {
        equipped = false;
        interactionRadius.SetActive(true);
    }

}
