using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorHandler : MonoBehaviour
{

    public Animator animator;
    public PlayerInputHandler input;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 vel = input.GetMoveInput();
        animator.SetFloat("VelX", vel.x);
        animator.SetFloat("VelY", vel.z);
        animator.SetFloat("VelMagnitude", vel.magnitude);
    }

}
