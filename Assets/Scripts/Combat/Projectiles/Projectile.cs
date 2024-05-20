using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected float speed;
    [Header("VFX")]
    [SerializeField] protected Transform trailTransform;
    [SerializeField] protected GameObject hitVFX;
    [SerializeField] protected float hitVFXOffset = 0.35f;

    //Variables to be set by Gun
    [HideInInspector] public Vector3 origin;
    [HideInInspector] public Vector3 destination;
    [HideInInspector] public float damage;
    [HideInInspector] public float maxDistance;

    //Cache
    protected Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Setup(Vector3 origin, Vector3 destination, float damage, float maxDistance)
    {
        this.origin = origin;
        this.destination = destination;
        this.damage = damage;
        this.maxDistance = maxDistance;
    }

    private void Start()
    {
        rb.velocity = (destination - transform.position).normalized * speed;
    }

    private void Update()
    {
        if (Vector3.Distance(origin, transform.position) > maxDistance)
        {
            //If Reached max distance and didn't hit anything
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
        OnHit(collision);
    }

    //Methods to Override
    protected abstract void OnHit(Collision collisionData);
}
