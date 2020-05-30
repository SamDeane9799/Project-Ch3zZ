﻿using System.Collections;
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

    //Set the position of the space, to be used when updating grid space for combat
    //and whatnot
    public void SetGridPosition(Vector2 pos)
    {
        grid_Position = pos;
        if (unit != null) unit.grid_Position = pos;
    }

    //Add a new character to the space and update their 
    //grid data to correspond appropriately
    public void AddCharacter(Character character)
    {
        unit = character;
        combat_Unit = character;
        character.grid_Position = grid_Position;
        character.transform.position = transform.position;
    }

    //Add a character to the space for the sole purpose of combat
    public void AddCombatCharacter(Character character)
    {
        combat_Unit = character;
        character.grid_Position = grid_Position;
    }

    //Remove a character from the space
    public void RemoveCharacter()
    {
        unit = null;
        combat_Unit = null;
    }

    //Reset the position of this grid's unit
    public void ResetUnitPosition()
    {
        unit.transform.position = transform.position;
    }

    //Reset the A* data costs of this space
    public void ResetCosts()
    {
        g = int.MaxValue;
        h = int.MaxValue;
        f = int.MaxValue;
    }
}
