using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGunConfig", menuName = "Weapons/New Gun Config", order = 2)]
public class GunConfig : ScriptableObject
{
    public string gunName;
    public GunType gunType;
    public bool continuiousFiring = false;
    [Header("Aim Data")]
    public bool useSniperMode = false;
    //public Sprite reticule;
    public GameObject reticule;
    [Header("Gun Stats")]
    public float baseDamage;
    public float baseFiringRate;
    public float baseRange;
    public int baseAmmoCapacity;
    public Vector3 baseRecoil;

    [Header("Weapon Override Level")]
    public int requiredOverrideLevel = 1;
    [Header("Animations")]
    public AnimatorOverrideController animatorOverrideController;

    //[Header("VFX")]
    //[Header("SFX")]

    //GunType Enum
    public enum GunType
    {
        Pistol,
        Assault,
        Sniper,
        Shotgun,
        Special
    }

}
