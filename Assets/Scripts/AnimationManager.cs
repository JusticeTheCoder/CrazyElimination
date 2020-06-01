using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    // Start is called before the first frame update
    public AnimationClip clockAnimation;
    public AnimationClip noAnimation;
    public AnimationClip explodeAnimation;
    private bool isPlaying;
    void Start()
    {

        isPlaying = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void AnimationPlay(string animationName = "clock")
    {
        if (!isPlaying)
        {
            isPlaying = true;
            StartCoroutine(AnimationCoroutine(animationName));
        }

    }

    private IEnumerator AnimationCoroutine(string animationName)
    {
        string stateName;
        float seconds;
        switch (animationName)
        {
            case "clock": stateName = clockAnimation.name; seconds = clockAnimation.length; break;
            case "explode": stateName = explodeAnimation.name; seconds = explodeAnimation.length; break;
            default: stateName = clockAnimation.name; seconds = clockAnimation.length; break;
        }

        Animator animator = GetComponent<Animator>();
        if (animationName == "explode")
            GetComponent<AudioSource>().Play(0);
        if (animator != null)
        {
            animator.Play(stateName);
            yield return new WaitForSeconds(seconds);
        }
        isPlaying = false;
        animator.Play(noAnimation.name);
    }
}
