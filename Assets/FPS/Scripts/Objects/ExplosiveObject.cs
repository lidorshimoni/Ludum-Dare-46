using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveObject : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Reference:")]
    public Transform ExplosionPosition;
    public GameObject ExplotionEffect;
    private DamageArea areaOfDamage;
    private Health m_Health;

    [Header("Explosion:")]
    public float force;
    public float damage;
    public LayerMask hittableLayers = -1;

    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

    private void Start()
    {
        areaOfDamage = gameObject.GetComponent<DamageArea>();
        m_Health = gameObject.GetComponent<Health>();

        //subscribe to health
        m_Health.onDie += OnDie;
        //m_Health.onDamaged += OnDamaged;
    }

    //void OnDamaged(float damage, GameObject damageSource)
    //{
    //    // test if the damage source is the player
    //    if (damageSource && damageSource.GetComponent<PlayerCharacterController>())
    //    {


    //    }
    //}

    void OnDie()
    {

        Debug.Log("BigSHIt");

        // spawn a particle system when dying
        var vfx = Instantiate(ExplotionEffect, ExplosionPosition.position, Quaternion.identity);
        Destroy(vfx, 5f);

        // area damage
        areaOfDamage.InflictDamageInArea(damage, transform.position, hittableLayers, k_TriggerInteraction, gameObject);

        //area knockback
        Collider[] colliders = Physics.OverlapSphere(transform.position, areaOfDamage.areaOfEffectDistance);
        foreach (Collider obj in colliders)
        {
            //if (obj.tag == "") ;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(force, transform.position, areaOfDamage.areaOfEffectDistance);
        }



        // this will call the OnDestroy function
        Destroy(gameObject);
    }


}
