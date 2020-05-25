using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bastionne : Character
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        origins.Add(ORIGIN.HUMAN);
        classes.Add(CLASS.BULWARK);
    }
    
    public override void Ultimate()
    {
    }
}
