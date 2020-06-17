using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror
{
    public class Shop : MonoBehaviour
    {
        //Instance of player to determine different parts of the shop
        public Player player;

        //Keep track of the characters in the shop and a list for all characters that could possibly be in there
        public List<ShopItem> shopItems;
        private List<ShopItem> itemsInShop;

        //Lists to keep track of the different items presented to the user
        public List<ItemUIElement> tierOneItems;
        private List<ItemUIElement> itemChoices;

        //Reference to our camera
        private Camera mainCamera;

        //These are all variables that depend on the player instance that we declared above
        private short[] chances;
        private short previousCurrency;
        private Text currencyTracker;
        private Text levelText;
        private Text xpProgress;
        private Slider xpSlider;

        void Start()
        {
            //Here is where we hook up all our variables
            //By using getchild we can find the gameObject that refers to different elements like Player level, xp slider, and player currency
            //All we have to do is pass in the index of the object in the getChild method to get a reference to its GameObject
            mainCamera = Camera.main;
            player = transform.parent.GetComponent<Player>();
            itemsInShop = new List<ShopItem>();
            itemChoices = new List<ItemUIElement>();
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

            //This is used to create a shop when the scene first starts
            NewShop();
            //PresentItems();
        }

        // Update is called once per frame
        void Update()
        {
            if (player.isLocalPlayer)
            {
                //here are our shop functions like buying and leveling
                //We make sure they have the correct amount of gold before leveling
                if (Input.GetKeyDown(KeyCode.D) && player.gold >= 2)
                {
                    NewShop();
                    player.gold -= 2;
                }

                //Here we check if the player has enough gold to level and we make sure they aren't already the max level
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
                //Here is where we check if the player is ready to level up
                if (player.level != 9 && player.xp >= Data.requiredXP[player.level - 2])
                {
                    LevelUp();
                }
                previousCurrency = player.gold;
            }
        }


        #region BUYING UNIT
        //When a unit is purchased this method is ran
        //It deletes the unit from the shop and will eventually put it on your bench
        private void Purchase()
        {
            ShopItem itemToPurchase = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<ShopItem>();
            short costOfItem = itemToPurchase.cost;
            //Check if player has room on bench
            if (costOfItem > player.gold) return;
            if (player.BuyUnit(itemToPurchase))
            {
                player.gold -= costOfItem;
                itemsInShop.Remove(itemToPurchase);
                Destroy(itemToPurchase.gameObject);
            }
        }

        //Returns what cost the unit should be that for each spot in the shop
        private int returnCost()
        {
            int randomNum = Random.Range(0, 101);
            int range = 0;
            for (int i = 0; i < chances.Length; i++)
            {
                range += chances[i];
                if (randomNum <= range)
                {
                    return i;
                }
            }
            return 5;
        }
        #endregion

        #region LEVELING
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
        #endregion

        #region ITEM IMPLEMENTATION

        //Method that presents 3 items
        //Later on we should specify which tier items to present in here
        private void PresentItems()
        {
            int[] indicesUsed = new int[3];
            for (int i = 0; i < indicesUsed.Length; i++)
            {
                indicesUsed[i] = -1;
            }
            for (int i = 0; i < 3; i++)
            {
                int index = Random.Range(0, tierOneItems.Count);

                while (ContainsIndex(indicesUsed, index))
                {
                    index = Random.Range(0, tierOneItems.Count);
                }
                indicesUsed[i] = index;

                ItemUIElement itemChoice = Instantiate<ItemUIElement>(tierOneItems[index]);
                itemChoices.Add(itemChoice);
                Button itemButton = itemChoice.GetComponent<Button>();
                itemButton.transform.SetParent(transform);
                itemButton.transform.localScale = Vector3.one;
                itemButton.transform.localRotation = Quaternion.Euler(Vector3.zero);
                itemButton.transform.GetChild(0).GetComponent<Text>().text = itemChoice.itemName.ToString();
                RectTransform itemTransform = itemChoice.GetComponent<RectTransform>();
                itemTransform.anchoredPosition3D = new Vector3(-75 + (i * 50), 0, 0);

                itemButton.onClick.AddListener(ItemPicked);
            }
        }

        //Checks if an int array contains the given variable
        private bool ContainsIndex(int[] numArray, int numToCheck)
        {
            if (numArray.Length != 0)
            {
                foreach (int i in numArray)
                {
                    if (numToCheck == i)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //The on click method for each option of button
        private void ItemPicked()
        {
            Item itemClicked = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<ItemUIElement>().itemPrefab;
            for (int i = 0; i < itemChoices.Count; i++)
            {
                if (itemChoices[i].itemName == itemClicked.itemName)
                {
                    player.AddItem(itemClicked);
                }
                Destroy(itemChoices[i].gameObject);
            }
            itemChoices.Clear();
        }
        #endregion

        #region HELPER METHODS
        //Method that is used to create a new shop
        void NewShop()
        {
            for (int i = 0; i < itemsInShop.Count; i++)
            {
                Destroy(itemsInShop[i].gameObject);
            }
            itemsInShop.Clear();
            for (int i = 0; i < 5; i++)
            {
                //To add an item to the shop we first instantiate it as a shopitem
                //Then we have to set its position and its parent while also making sure its scale isnt too high and the rotation is zeroed out
                itemsInShop.Add(Instantiate<ShopItem>(shopItems[returnCost()]));
                Button itemButton = itemsInShop[i].GetComponent<Button>();
                itemButton.transform.SetParent(transform);
                itemButton.transform.localScale = Vector3.one;
                itemButton.transform.localRotation = Quaternion.Euler(Vector3.zero);
                RectTransform buttonTransform = itemButton.GetComponent<RectTransform>();
                buttonTransform.anchoredPosition3D = new Vector3(200 + (i * buttonTransform.rect.width), buttonTransform.rect.height / 2, 0);

                //here we add the method that we want to happen when a player clicks it
                itemButton.onClick.AddListener(Purchase);
            }
        }

        //When currency is changed do this
        private void CurrencyChanged()
        {
            currencyTracker.text = player.gold.ToString();
        }
        #endregion
    }
}