﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helveller : Character
{
    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        attributes.Add(global::ATTRIBUTES.FELWALKER);
        attributes.Add(ATTRIBUTES.BLIGHTCRAFTER);
        ID = 2;
    }

    public override void Ultimate()
    {
    }
}
