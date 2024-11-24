using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakePlayerControl : MonoBehaviour
{
    private Animator animator;

    [Tooltip("The real player object")]
    public PlayerMovement realPlayerMovement;

    private Transform realPlayerTransform;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        realPlayerTransform = realPlayerMovement.transform.Find("Rio");
    }

    // Update is called once per frame
    void Update()
    {
        // synchronize rotation and postiton
        transform.rotation = realPlayerTransform.rotation;
        transform.position = realPlayerTransform.position;
        
        // synchronize animation
        PlayerMovement.AnimationVars animationVars = realPlayerMovement.getAnimationVars();
        
        animator.SetBool("grounded", animationVars.grounded);
        animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
        animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
        animator.SetBool("requestJump", animationVars.requestJump);
        animator.SetBool("sliding", animationVars.sliding);
        animator.SetBool("paragliding", animationVars.paragliding);
        animator.SetBool("horizontalInput", animationVars.horizontalInput);
        animator.SetBool("sprinting", animationVars.sprinting);
        animator.SetBool("holdingMap", animationVars.holdingMap);
    }
}
