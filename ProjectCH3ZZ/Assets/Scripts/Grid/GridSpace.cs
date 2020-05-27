using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpace : MonoBehaviour
{
    public Character unit;
    public Character combat_Unit;
    public Vector2 grid_Position;

    public void SetGridPosition(Vector2 pos)
    {
        grid_Position = pos;
        unit.grid_Position = pos;
    }

    public void AddCharacter(Character character)
    {
        unit = character;
        combat_Unit = character;
        character.transform.position = transform.position;
    }

    public void AddCombatCharacter(Character character)
    {
        combat_Unit = character;
        character.transform.position = transform.position;
        character.grid_Position = grid_Position;
    }

    public void RemoveCharacter()
    {
        unit = null;
        combat_Unit = null;
    }

    public void ResetUnitPosition()
    {
        unit.transform.position = transform.position;
    }
}
