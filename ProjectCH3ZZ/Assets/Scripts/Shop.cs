using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    // Start is called before the first frame update
    public Player player;
    public List<ShopItem> shopItems;
    private List<ShopItem> itemsInShop = new List<ShopItem>();
    private Camera mainCamera;
    public Canvas shopCanvas;
    public int[] chances;
    private int previousCurrency;
    private Text currencyTracker;
    private Text levelText;
    private Text xpProgress;
    private Slider xpSlider;

    public int playerCurrency = 50;
    public int playerLevel;
    public int playerCurrentXP = 14;
    void Start()
    {
        mainCamera = Camera.main;
        levelText = GameObject.Find("Level").GetComponent<Text>();
        levelText.text = playerLevel.ToString();
        xpSlider = GameObject.Find("XpBar").GetComponent<Slider>();
        if (playerLevel != 9)
        {
            xpSlider.maxValue = Data.requiredXP[playerLevel - 2];
            xpSlider.value = playerCurrentXP;
        }
        else
        {
            xpSlider.maxValue = 1;
            xpSlider.value = 1;
        }
        xpProgress = GameObject.Find("XPProgress").GetComponent<Text>();
        xpProgress.text = playerCurrentXP.ToString() + "/" + Data.requiredXP[playerLevel - 2];
        chances = Data.rollChancesByLevel[playerLevel - 2];
        previousCurrency = playerCurrency;
        currencyTracker = GameObject.Find("Currency").GetComponent<Text>();
        currencyTracker.text = playerCurrency.ToString();
        NewShop();
    }

    // Update is called once per frame
    void Update()
    {
        //here are our shop functions like buying and leveling
        //We make sure they have the correct amount of gold before leveling
        if (Input.GetKeyDown(KeyCode.D) && playerCurrency >= 2)
        {
            NewShop();
            playerCurrency -= 2;
        }
        else if (Input.GetKeyDown(KeyCode.F) && playerCurrency >= 4 && playerLevel != 9)
        {
            //Add to players experience
            playerCurrency -= 4;
            playerCurrentXP += 4;
            xpSlider.value = playerCurrentXP;
            xpProgress.text = playerCurrentXP.ToString() + "/" + Data.requiredXP[playerLevel - 2];
        }

        //Here we check if we need to update any of  the costs
        if (previousCurrency != playerCurrency)
        {
            CurrencyChanged();
        }
        if (playerLevel != 9 && playerCurrentXP >= Data.requiredXP[playerLevel - 2])
        {
            LevelUp();
        }
        previousCurrency = playerCurrency;
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
            itemButton.transform.SetParent(shopCanvas.transform);
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
        int costOfItem = itemToPurchase.cost;
        //Check if player has room on bench
        if (costOfItem <= playerCurrency && player.AddToBench(itemToPurchase.characterPrefab))
        {
            playerCurrency -= costOfItem;
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
        currencyTracker.text = playerCurrency.ToString();
    }

    //When a player levels up do this
    //Up their level, change their role chance, give the player another unit slot, add any extra xp to the next level, update the xp bar and xp text.
    private void LevelUp()
    {
        playerLevel++;
        levelText.text = playerLevel.ToString();
        chances = Data.rollChancesByLevel[playerLevel - 2];
        if (playerLevel != 9)
        {
            playerCurrentXP -= Data.requiredXP[playerLevel - 3];
            xpSlider.value = playerCurrentXP;
            xpSlider.maxValue = Data.requiredXP[playerLevel - 2];
            xpProgress.text = playerCurrentXP.ToString() + "/" + Data.requiredXP[playerLevel - 2];
        }
        else
        {
            xpProgress.text = "MAX";
        }
    }
}
