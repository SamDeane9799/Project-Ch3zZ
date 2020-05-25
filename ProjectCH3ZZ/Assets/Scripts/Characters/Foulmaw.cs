using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foulmaw : Character
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        attributes.Add(global::ATTRIBUTES.BEAST);
        attributes.Add(ATTRIBUTES.BLIGHTCRAFTER);
    }

    public override void Ultimate()
    {
    }
}
