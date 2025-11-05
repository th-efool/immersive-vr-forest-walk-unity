using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemoKitStylizedAnimatedDogs
{
public class DemoController : MonoBehaviour
{
    [SerializeField] private List<AnimationButton> _buttons;
    [SerializeField] private List<Animator> _animators;

    private void Start()
    {
       foreach(var button in _buttons)
       {
          button.Click += OnAnimationButtonClick;
       }
    }

    private void OnAnimationButtonClick(int id)
    {
       foreach(var animator in _animators)
       {
          animator.SetInteger("AnimationID",id);
       }
    }
}
}