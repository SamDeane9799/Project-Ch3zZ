using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpace : MonoBehaviour
{
    // --- GRID DATA ---
    [Header("Grid Data")]
    public Character unit;
    public Character combat_Unit;
    public Vector2 grid_Position;

    // --- A* DATA ---
    [Header("A* Data")]
    public Vector2 parent_Position;
    public float g;
    public float f;
    public float h;

    public void SetGridPosition(Vector2 pos)
    {
        grid_Position = pos;
        if (unit != null) unit.grid_Position = pos;
    }

    public void AddCharacter(Character character)
    {
        unit = character;
        combat_Unit = character;
        character.grid_Position = grid_Position;
        character.transform.position = transform.position;
    }

    public void AddCombatCharacter(Character character)
    {
        combat_Unit = character;
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

    public void ResetCosts()
    {
        g = int.MaxValue;
        h = int.MaxValue;
        f = int.MaxValue;
    }
}
