using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class TestPlayer : Player
{
    public Character characterPrefab;

    // Start is called before the first frame update
    public override void Awake()
    {
        p_Attributes = new Dictionary<ATTRIBUTES, short>();
        field_Units = new List<Character>();
        bench_Units = new List<Character>();
        current_Mods = new CHARACTER_MODIFIER[19]; //Number of possible mods

        grid = new GridSpace[8, 4];
        bench = new GridSpace[8];

        for (short i = 0; i < 4; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                grid[j, i] = Instantiate<GridSpace>(gridPrefab, new Vector3(j - 3.5f, 5, i - 1f), Quaternion.identity);
            }
        }
        for (short i = 0; i < 8; i++)
        {
            bench[i] = Instantiate<GridSpace>(gridPrefab, new Vector3(i - 3.5f, 5,  3.5f), Quaternion.identity);
        }

        Character character = Instantiate<Character>(characterPrefab);
        BenchToField(character);
        grid[7, 3].AddCharacter(character);
        
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[6, 3].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[5, 2].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[4, 2].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[3, 1].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[2, 1].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[1, 0].AddCharacter(character);
        //
        //character = Instantiate<Character>(characterPrefab);
        //BenchToField(character);
        //grid[0, 0].AddCharacter(character);
    }

    public override void Update()
    {
        //im zoe
    }

    protected override void BenchToField(Character unit)
    {
        bench_Units.Remove(unit);
        field_Units.Add(unit);
        foreach (Character c in field_Units)
        {
            if (unit.name == c.name && c != unit)
            {
                return;
            }
        }
        foreach (ATTRIBUTES o in unit.attributes)
        {
            if (p_Attributes.ContainsKey(o)) p_Attributes[o]++;
            else p_Attributes.Add(o, 1);
            CheckAttributes(o);
        }
    }

    protected override void FieldToBench(Character unit)
    {
        field_Units.Remove(unit);
        bench_Units.Add(unit);
        foreach (Character c in field_Units)
        {
            if (unit.name == c.name && unit != c)
            {
                return;
            }
        }
        foreach (ATTRIBUTES o in unit.attributes)
        {
            if (p_Attributes.ContainsKey(o)) p_Attributes[o]--;
            CheckAttributes(o);
            if (p_Attributes[o] == 0) p_Attributes.Remove(o);
        }
    }
}