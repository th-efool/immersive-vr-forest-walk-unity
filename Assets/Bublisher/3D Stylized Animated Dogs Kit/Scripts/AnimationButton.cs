using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKitStylizedAnimatedDogs
{

[RequireComponent(typeof(Button))]
public class AnimationButton : MonoBehaviour
{
    [SerializeField] public int _animationID;

    public event Action<int> Click;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(()=>{Click?.Invoke(_animationID);});
    }
}

}
