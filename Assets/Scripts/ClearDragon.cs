using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearDragon : MonoBehaviour
{
    public AnimationClip clearAnimation;

    private bool isClearing;

    public bool IsClearing { get => isClearing; }

    protected GameDragon dragon;

    public virtual void Clear()
    {
        isClearing = true;
        StartCoroutine(ClearCoroutine());
    }

    private IEnumerator ClearCoroutine()
    {
        Animator animator = GetComponent<Animator>();
        if(animator != null)
        {
            animator.Play(clearAnimation.name);
            yield return new WaitForSeconds(clearAnimation.length);
            Destroy(gameObject);
        }
    }

}
