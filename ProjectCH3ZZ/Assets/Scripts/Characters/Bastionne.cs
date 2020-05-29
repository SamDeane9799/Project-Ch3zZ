﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bastionne : Character
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        attributes.Add(global::ATTRIBUTES.HUMAN);
        attributes.Add(ATTRIBUTES.BULWARK);
        SetStats(1, 1, 1, 120, 0, 40, 100, 0.6f, 650, 40, 20, 1);
    }
    
    public override void Ultimate()
    {
    }

    public override void IncrementLevel()
    {
        base.IncrementLevel();
        if (level == 2)
        {
            maxHealth = 1170;
            attack_Damage = 72;
        }
        else
        {
            maxHealth = 2106;
            attack_Damage = 144;
        }
    }
}
