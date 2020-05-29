using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    // Start is called before the first frame update
    public Player player;
    public List<ShopItem> shopItems;
    private List<ShopItem> itemsInShop;
    public List<Item> tierOneItems;
    private List<Item> itemChoices;
    private Camera mainCamera;
    public short[] chances;
    private short previousCurrency;
    private Text currencyTracker;
    private Text levelText;
    private Text xpProgress;
    private Slider xpSlider;

    void Start()
    {
        mainCamera = Camera.main;
        itemsInShop = new List<ShopItem>();
        itemChoices = new List<Item>();
        levelText = transform.GetChild(2).transform.GetChild(2).GetComponent<Text>();
        levelText.text = player.level.ToString();
        xpSlider = transform.GetChild(2).GetComponent<Slider>();
        if (player.level != 9)
        {
            xpSlider.maxValue = Data.requiredXP[player.level - 2];
            xpSlider.value = player.xp;
        }
        else
        {
            xpSlider.maxValue = 1;
            xpSlider.value = 1;
        }
        xpProgress = transform.GetChild(2).GetChild(3).GetComponent<Text>();
        xpProgress.text = player.xp.ToString() + "/" + Data.requiredXP[player.level - 2];
        chances = Data.rollChancesByLevel[player.level - 2];
        previousCurrency = player.gold;
        currencyTracker = transform.GetChild(1).GetComponent<Text>();
        currencyTracker.text = player.gold.ToString();
        NewShop();
        //PresentItems();
    }

    // Update is called once per frame
    void Update()
    {
        //here are our shop functions like buying and leveling
        //We make sure they have the correct amount of gold before leveling
        if (Input.GetKeyDown(KeyCode.D) && player.gold >= 2)
        {
            NewShop();
            player.gold -= 2;
        }
        else if (Input.GetKeyDown(KeyCode.F) && player.gold >= 4 && player.level != 9)
        {
            //Add to players experience
            player.gold -= 4;
            player.xp += 4;
            xpSlider.value = player.xp;
            xpProgress.text = player.xp.ToString() + "/" + Data.requiredXP[player.level - 2];
        }

        //Here we check if we need to update any of  the costs
        if (previousCurrency != player.gold)
        {
            CurrencyChanged();
        }
        if (player.level != 9 && player.xp >= Data.requiredXP[player.level - 2])
        {
            LevelUp();
        }
        previousCurrency = player.gold;
    }

    //Method that is used to create a new shop
    void NewShop()
    {
        for(int i = 0; i < itemsInShop.Count; i++)
        {
            Destroy(itemsInShop[i].gameObject);
        }
        itemsInShop.Clear();
        for(int i = 0; i < 5; i++)
        {
            itemsInShop.Add(Instantiate<ShopItem>(shopItems[returnCost()]));
            Button itemButton = itemsInShop[i].GetComponent<Button>();
            itemButton.transform.SetParent(transform);
            itemButton.transform.localScale = Vector3.one;
            itemButton.transform.localRotation = Quaternion.Euler(Vector3.zero);
            RectTransform buttonTransform = itemButton.GetComponent<RectTransform>();
            buttonTransform.anchoredPosition3D = new Vector3(200 + (i * buttonTransform.rect.width), buttonTransform.rect.height/2, 0);
            itemButton.onClick.AddListener(Purchase);
        }
    }

    //When a unit is purchased this method is ran
    //It deletes the unit from the shop and will eventually put it on your bench
    private void Purchase()
    {
        ShopItem itemToPurchase = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<ShopItem>();
        short costOfItem = itemToPurchase.cost;
        //Check if player has room on bench
        if (costOfItem <= player.gold && player.AddToBench(itemToPurchase.characterPrefab))
        {
            player.gold -= costOfItem;
            itemsInShop.Remove(itemToPurchase);
            //Add character to players list
            Destroy(itemToPurchase.gameObject);
        }
    }

    //Returns what cost the unit should be that for each spot in the shop
    private int returnCost()
    {
        int randomNum = Random.Range(0, 101);
        int range = 0;
        for(int i = 0; i < chances.Length; i++)
        {
            range += chances[i];
            if(randomNum <= range)
            {
                return i;
            }
        }
        return 5;
    }

    //When currency is changed do this
    private void CurrencyChanged()
    {
        currencyTracker.text = player.gold.ToString();
    }

    //When a player levels up do this
    //Up their level, change their role chance, give the player another unit slot, add any extra xp to the next level, update the xp bar and xp text.
    private void LevelUp()
    {
        player.level++;
        levelText.text = player.level.ToString();
        chances = Data.rollChancesByLevel[player.level - 2];
        if (player.level != 9)
        {
            player.xp -= Data.requiredXP[player.level - 3];
            xpSlider.value = player.xp;
            xpSlider.maxValue = Data.requiredXP[player.level - 2];
            xpProgress.text = player.xp.ToString() + "/" + Data.requiredXP[player.level - 2];
        }
        else
        {
            xpProgress.text = "MAX";
        }
    }

    private void PresentItems()
    {
        for(int i = 0; i < 3; i++)
        {
            Item itemChoice = Instantiate<Item>(tierOneItems[Random.Range(0, tierOneItems.Count)]);
            itemChoices.Add(itemChoice);
            itemChoice.transform.SetParent(transform);
            itemChoice.transform.localScale = Vector3.one;
            itemChoice.transform.localRotation = Quaternion.Euler(Vector3.zero);            
            itemChoice.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i * 60 - 60, 0, 0);
        }
    }

    public void ClearItems()
    {
        for(int i = 0; i < itemChoices.Count; i++)
        {
            Destroy(itemChoices[i].gameObject);
        }
        itemChoices.Clear();
    }

    public void RemoveItemFromChoice(Item itemToRemove)
    {
        itemChoices.Remove(itemToRemove);
    }
}
