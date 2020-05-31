using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    // Start is called before the first frame update
    public AnimationClip clockAnimation;
    public AnimationClip noAnimation;
    private bool isPlaying;
    void Start()
    {
        isPlaying = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ClockPlay()
    {
        if (!isPlaying)
        {
            isPlaying = true;
            StartCoroutine(ClockCoroutine());
        }

    }

    private IEnumerator ClockCoroutine()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(clockAnimation.name);
            yield return new WaitForSeconds(clockAnimation.length);
        }
        isPlaying = false;
        animator.Play(noAnimation.name);
    }
}
