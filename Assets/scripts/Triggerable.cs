using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triggerable : MonoBehaviour
{
    public bool isTriggered;

    public virtual void beTriggered(MonoBehaviour trigger){
        isTriggered = !isTriggered;
    }


}