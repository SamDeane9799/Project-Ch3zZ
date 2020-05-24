using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpace : MonoBehaviour
{
    public GameObject unit;

    public void AddCharacter(GameObject character)
    {
        unit = character;
        character.transform.position = transform.position;
    }

    public void RemoveCharacter()
    {
        unit = null;
    }

    public void ResetUnitPosition()
    {
        unit.transform.position = transform.position;
    }
}
