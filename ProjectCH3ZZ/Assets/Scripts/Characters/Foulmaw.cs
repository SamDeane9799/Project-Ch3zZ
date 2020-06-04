using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class Foulmaw : Character
    {
        // Start is called before the first frame update
        public override void Awake()
        {
            base.Awake();
            attributes.Add(ATTRIBUTES.BEAST);
            attributes.Add(ATTRIBUTES.BLIGHTCRAFTER);
            ID = 1;
        }

        public override void Ultimate()
        {
        }
    }
}