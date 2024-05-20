using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultProjectile : Projectile
{
    protected override void OnHit(Collision collisionData)
    {
        //If ColliderHit has health Deal Damage first.
        Vector3 contactPoint = collisionData.GetContact(0).point;
        Vector3 contactNormal = collisionData.GetContact(0).normal;

        Vector3 spawnPoint = contactPoint + (contactNormal.normalized * hitVFXOffset);
        GameObject hitVFXInstance = Instantiate(hitVFX, spawnPoint, Quaternion.identity);
        hitVFXInstance.transform.forward = contactNormal;

        trailTransform.parent = null;
        Destroy(gameObject);
    }

}
