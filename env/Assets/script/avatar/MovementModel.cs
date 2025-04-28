using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovementModel : MonoBehaviour
{
    protected bool isStopped = false;
    public bool IsStopped
    {
        set { isStopped = value; }
    }

    public abstract void StartWalking();

}
