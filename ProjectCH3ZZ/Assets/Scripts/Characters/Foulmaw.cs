using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foulmaw : Character
{
    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        attributes.Add(global::ATTRIBUTES.BEAST);
        attributes.Add(ATTRIBUTES.BLIGHTCRAFTER);
        ID = 1;
    }

    public override void Ultimate()
    {
    }
}
