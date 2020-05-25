using System.Collections;
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
        health = 100;
        for(int i = 0; i < 3; i++)
        {
            TakeDamage();
        }
    }
    
    public override void Ultimate()
    {
    }
}
