using UnityEngine;
using UnityEngine.Events;

public class AnimatorHelper : MonoBehaviour
{
    public Animator animator;

    internal UnityAction animatorCallback = () => { };

    public void OnEndAnimation()
    {
        animatorCallback();
    }

    public void Animate(string name, UnityAction robotCallback)
    {
        animatorCallback = robotCallback;
        animator.Play(name);
    }
}