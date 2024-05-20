using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretAI : MonoBehaviour
{
    [Header("Transform & GameObjects")]
    [SerializeField] Transform turretHeadRotationTransform;
    [SerializeField] Transform playerShootTarget;
    [SerializeField] Transform shootPointTransform;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] GameObject muzzleFlash;
    [Header("Values")]
    [SerializeField] float rotationSpeed;
    [SerializeField] float sphereCastRadius = 3f;
    [SerializeField] float sphereCastMaxDistance = 50;
    [SerializeField] float timeBetweenShots = 0.15f;
    [SerializeField] int maxAmmo = 250;


    bool attack = false;
    bool canFire = true;
    float currentAmmo;

    private void Start()
    {
        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        if (attack)
        {
            RotateTowardToTarget();
        }
    }

    void RotateTowardToTarget()
    {
        Vector3 lookDirection = (playerShootTarget.position - turretHeadRotationTransform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        turretHeadRotationTransform.rotation = Quaternion.Slerp(turretHeadRotationTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        turretHeadRotationTransform.rotation = Quaternion.Euler(0, turretHeadRotationTransform.rotation.eulerAngles.y, 0);

        RaycastHit[] hitInfo = Physics.SphereCastAll(turretHeadRotationTransform.position, sphereCastRadius, turretHeadRotationTransform.forward, sphereCastMaxDistance);

        foreach (RaycastHit hit in hitInfo)
        {
            if (hit.collider.CompareTag("Player"))
            {
                Fire();
                break;
            }
        }
    }

    void Fire()
    {
        if (canFire && currentAmmo > 0)
        {
            //Spawn Projectile
            GameObject projectileObj = Instantiate(projectilePrefab, shootPointTransform.position, shootPointTransform.rotation);
            Ray ray = new Ray(turretHeadRotationTransform.position, turretHeadRotationTransform.forward);
            Vector3 projectileDestination = ray.GetPoint(sphereCastMaxDistance);

            Projectile projectileClass = projectileObj.GetComponent<Projectile>();
            projectileClass.Setup(shootPointTransform.position, projectileDestination, 20, sphereCastMaxDistance);

            //Reduce Ammo
            currentAmmo = currentAmmo - 1;

            //Start Corotines
            StartCoroutine(FireRateRoutine());

            if (!muzzleFlash.activeInHierarchy)
                StartCoroutine(MuzzleFlashRoutine());
        }
    }
    //Coroutines
    IEnumerator MuzzleFlashRoutine()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        muzzleFlash.SetActive(false);
    }

    IEnumerator FireRateRoutine()
    {
        canFire = false;
        yield return new WaitForSeconds(timeBetweenShots);
        canFire = true;
    }


    //Triggers

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            attack = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            attack = false;
        }
    }
}
