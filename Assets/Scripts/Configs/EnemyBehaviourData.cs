using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Behaviour Data", menuName = "Enemy/New Behaviour Data", order = 1)]
public class EnemyBehaviourData : ScriptableObject
{
    public bool canMelee = true;
    public bool canTakeCover = true;
    public bool shouldAlwaysRemainInCover = false;
    public AttackRange preferredAttackRange;
    public List<AttackRange> otherFlexibleRanges = new List<AttackRange>();

    public enum AttackRange
    {
        Melee,
        Close,
        Mid,
        Far
    }
}
