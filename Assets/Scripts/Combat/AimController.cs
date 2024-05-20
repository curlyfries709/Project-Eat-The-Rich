using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using DG.Tweening;

public class AimController : MonoBehaviour
{
    [Header("Reticule")]
    [SerializeField] GameObject reticuleHeader;
    [SerializeField] Image reticuleImage;
    [Header("Cameras")]
    [SerializeField] GameObject aimMainCamera;
    [SerializeField] GameObject aimHighCoverLCam;
    [SerializeField] GameObject aimHighCoverRCam;
    [Header("Rigging")]
    [SerializeField] Rig aimRig;
    [SerializeField] Transform aimRigTarget;
    [Header("Values")]
    [SerializeField] float aimTargetDistance = 40f;
    [SerializeField] float aimWeightChangeSpeed = 20f;
    [SerializeField] float getPointOnRayThatDidntHit = 10f;
    [SerializeField] float shootFromCoverOffset = 1f;
    [Header("Clamps")]
    [SerializeField] float highCoverAimAngleChange = 50f;
    [SerializeField] float lowCoverAimAngleChange = 60f;



    //Variables
    Vector3 projectileDestination;
    Vector3 shootFromHighCoverPos;
    Vector3 highCornerReturnPos;

    AimMode currentAimMode = AimMode.Default;

    //Bools
    bool continiousFireTriggered = false;
    bool aimingFromHighCover = false;
    bool aimFromRight = false;
    bool poppedOut = false;
    bool activateFocus = false;

    //Caches
    Camera mainCam;
    PlayerInput playerInput;
    BulletTime bulletTime;

    private enum AimMode
    {
        Default,
        Sniper,
        Throw
    }
    private void Awake()
    {
        aimMainCamera.SetActive(false);
        mainCam = Camera.main;
        bulletTime = GetComponent<BulletTime>();
        this.enabled = false;
    }

    private void OnEnable()
    {
        if (!playerInput)
        {
            playerInput = ControlsManager.Instance.GetPlayerInput();
        }

        PopOutAndShoot();

        //Allow Firing While Aiming after transistion occured.
        StartCoroutine(FireDelayRoutine());

        //Reticule enabled in FireDelayRoutine
        SetReticule();
        //reticuleImage.sprite = InventoryManager.Instance.GetCurrentEquippedGun().GetGunConfig().reticule;

        //Activate Aiming
        ActivateAimCam(true);
    }

    private void Update()
    {
        if(InventoryManager.Instance.GetCurrentEquippedGun())
        {
            //Update Cams
            ActivateAimCam(true);
            //Update Current Gun Reticule
            //reticuleImage.sprite = InventoryManager.Instance.GetCurrentEquippedGun().GetGunConfig().reticule;
            bulletTime.Focus(activateFocus, reticuleHeader.activeInHierarchy);

            Debug.DrawRay(mainCam.transform.position, mainCam.transform.forward * 999, Color.green);

            CorrectReticulePos();
            
            SetAimTargetPos();

            if (continiousFireTriggered)
            {
                Fire();
            }
        }
    }

    private void OnDisable()
    {
        //Disable Firing While not Aiming
        continiousFireTriggered = false;

        activateFocus = false;
        bulletTime.Focus(activateFocus, false);

        StopCoroutine(FireDelayRoutine());

        if (aimingFromHighCover && poppedOut)
        {
            //Pop back into Cover
            transform.DOMove(highCornerReturnPos, 0.25f);
            poppedOut = false;
        }

        if (playerInput)
        {
            playerInput.onActionTriggered -= OnFire;
            playerInput.onActionTriggered -= OnFocus;
        }

        ActivateAimCam(false);
        reticuleHeader.SetActive(false);

        aimingFromHighCover = false;
        aimFromRight = false;
    }

    public void ShootFromHighCoverSetup(Vector3 direction, bool inHighCover, bool aimFromRight)
    {
        shootFromHighCoverPos = transform.position + (direction.normalized * shootFromCoverOffset);
        aimingFromHighCover = inHighCover;
        this.aimFromRight = aimFromRight;
    }

    private void ActivateAimCam(bool value)
    {
        aimMainCamera.SetActive(value && !aimingFromHighCover);

        aimHighCoverRCam.SetActive(value && aimFromRight && aimingFromHighCover);
        aimHighCoverLCam.SetActive(value && !aimFromRight && aimingFromHighCover);
    }

    private void PopOutAndShoot()
    {
        if (aimingFromHighCover)
        {
            poppedOut = true;
            highCornerReturnPos = transform.position;
            transform.DOMove(shootFromHighCoverPos, 0.25f);
        }
    }

    private void Fire()
    {
        Gun currentGun = InventoryManager.Instance.GetCurrentEquippedGun();

        if (currentGun.GetAmmoCount() > 0)
        {
            currentGun.Shoot(projectileDestination);
        }
    }

    private void CorrectReticulePos()
    {
        RaycastHit hitInfo;
        Vector3 projectileDirection;
        Vector3 projectileOrigin = InventoryManager.Instance.GetCurrentEquippedGun().GetShootPoint().position;

        Ray shootDirection = new Ray(mainCam.transform.position, mainCam.transform.forward);

        //Predict where Projectile is going to hit.
        if (Physics.Raycast(shootDirection, out hitInfo))
        {
            projectileDirection = (hitInfo.point - projectileOrigin).normalized;
        }
        else
        {
            projectileDirection = (shootDirection.GetPoint(getPointOnRayThatDidntHit) - projectileOrigin).normalized;
        }

        //Update Reticule Pos
        Ray projecileDirectionRay = new Ray(projectileOrigin, projectileDirection);

        if (Physics.Raycast(projectileOrigin, projectileDirection, out hitInfo))
        {
            projectileDestination = hitInfo.point;
        }
        else
        {
            projectileDestination = projecileDirectionRay.GetPoint(getPointOnRayThatDidntHit);
        }

        Debug.DrawRay(projectileOrigin, projectileDirection * 999, Color.blue);

        Vector3 screenPos = mainCam.WorldToScreenPoint(projectileDestination);
        reticuleImage.rectTransform.position = screenPos;
    }

    private void SetReticule()
    {
        foreach (Transform child in reticuleHeader.transform)
        {
            Destroy(child.gameObject);
        }

        GameObject reticule = Instantiate(InventoryManager.Instance.GetCurrentEquippedGun().GetGunConfig().reticule, reticuleHeader.transform);
        reticuleImage = reticule.GetComponentInChildren<Image>();
    }

    public void SetRigWeight(float targetWeight)
    {
        //Called by a Player State.
        aimRig.weight = Mathf.Lerp(aimRig.weight, targetWeight, Time.deltaTime * aimWeightChangeSpeed);
    }

    private void SetAimTargetPos()
    {
        Ray aimTargetRay = new Ray(mainCam.transform.position, mainCam.transform.forward);
        aimRigTarget.transform.position = aimTargetRay.GetPoint(aimTargetDistance);
    }

    IEnumerator FireDelayRoutine()
    {
        yield return new WaitForSeconds(Time.deltaTime * aimWeightChangeSpeed);
        reticuleHeader.SetActive(true);
        playerInput.onActionTriggered += OnFire;
        playerInput.onActionTriggered += OnFocus;
    }
    //Getter
    public bool IsAimingFromHighCover()
    {
        return aimingFromHighCover && this.enabled;
    }

    public float GetHighCoverAngleChange()
    {
        return highCoverAimAngleChange;
    }

    public float GetLowCoverAngleChange()
    {
        return lowCoverAimAngleChange;
    }

    //Input
    private void OnFire(InputAction.CallbackContext context)
    {
        if (context.action.name != "Fire") return;

        Gun currentGun = InventoryManager.Instance.GetCurrentEquippedGun();

        if (context.performed && currentGun)
        {
            if (currentGun.GetGunConfig().continuiousFiring)
            {
                continiousFireTriggered = true;
            }
            else
            {
                Fire();
            }
        }
        else if (context.canceled)
        {
            continiousFireTriggered = false;
        }
    }

    private void OnFocus(InputAction.CallbackContext context)
    {
        if (context.action.name != "Focus") return;

        Gun currentGun = InventoryManager.Instance.GetCurrentEquippedGun();

        if (context.performed && currentGun)
        {
            activateFocus = true;
        }
        else if (context.canceled)
        {
            activateFocus = false;
        }
    }
}
