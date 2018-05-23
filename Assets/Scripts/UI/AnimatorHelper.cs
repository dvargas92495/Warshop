using UnityEngine;
using UnityEngine.Events;

public class AnimatorHelper : MonoBehaviour
{
    internal UnityAction animatorCallback = () => { };

    public void OnEndAnimation()
    {
        animatorCallback();
    }
}