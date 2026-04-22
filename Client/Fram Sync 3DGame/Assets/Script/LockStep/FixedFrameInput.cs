using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedFrameInput
{
    public float Vertical;
    public float Horizontal;
    public bool Jump = false;
    public void Reset()
    {
        Vertical=0; Horizontal = 0;
        Jump = false;
    }
}
