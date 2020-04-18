using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Reward { Health, Ammo}

public class Consumable : MonoBehaviour
{
    public Reward rewardType;

    public float amount=0; 

    public void Consume(GameObject player)
    {
        if (rewardType == Reward.Health)
            player.GetComponent<Health>().Heal(amount);
    }

}
