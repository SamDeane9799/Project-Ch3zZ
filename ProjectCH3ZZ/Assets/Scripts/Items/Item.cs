using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror
{
    public enum ITEMNAME
    {
        siphoner,
        jaggedBlade,
        penetratingBullets
    }
    public class Item : MonoBehaviour
    {
        public ITEMNAME itemName;

    }
}