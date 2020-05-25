using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helveller : Character
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        attributes.Add(global::ATTRIBUTES.FELWALKER);
        attributes.Add(ATTRIBUTES.BLIGHTCRAFTER);
    }

    public override void Ultimate()
    {
    }
}
