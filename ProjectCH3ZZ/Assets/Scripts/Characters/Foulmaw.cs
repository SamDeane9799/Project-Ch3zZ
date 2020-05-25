using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foulmaw : Character
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        origins.Add(ORIGIN.BEAST);
        classes.Add(CLASS.BLIGHTCRAFTER);
    }

    public override void Ultimate()
    {
    }
}
