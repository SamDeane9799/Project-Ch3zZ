using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    enum GAME_PHASE
    {
        PREPARATION,
        COMBAT,
        ITEM,
        CHANGE,
    }

    public class GameManager : NetworkManager
    {
        [Header("Player Data")]
        // --- PLAYER DATA --- 
        public GameObject player_Prefab;
        public GameObject test_Prefab;
        private List<Player> players;
        private List<CombatManager> combat;
        public List<GameObject> characterPrefabs;

        // --- GAME PHASE DATA ---
        private GAME_PHASE current_Phase;
        private GAME_PHASE previous_Phase;
        private float phase_Timer;
        private short round;        
        private const float PREPARATION_TIME = 2;
        private const float ITEM_TIME = 20;
        private const float PHASE_CHANGE_TIME = 5;
        private const float COMBAT_TIME = 10;

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            players = new List<Player>();
            combat = new List<CombatManager>();
            for (int i = 0; i < 1; i++)
            {
                combat.Add(new CombatManager());
            }
            current_Phase = GAME_PHASE.PREPARATION;

            //poop
        }

        // Update is called once per frame
        void Update()
        {
            if (players.Count < 2)
                return;

            //Check the current phase of the game
            switch (current_Phase)
            {
                case GAME_PHASE.PREPARATION: //Preparation of the game
                    if (phase_Timer >= PREPARATION_TIME)
                    {
                        PhaseChange();
                    }
                    break;
                case GAME_PHASE.COMBAT: //Combat phase
                    foreach (CombatManager c in combat)
                    {
                        c.Update();
                    }
                    if (phase_Timer >= COMBAT_TIME)
                    {
                        PhaseChange();
                    }
                    break;
                case GAME_PHASE.CHANGE: //Brief change phase 
                    if (phase_Timer >= PHASE_CHANGE_TIME)
                    {
                        switch (previous_Phase)
                        {
                            case GAME_PHASE.PREPARATION:
                                current_Phase = GAME_PHASE.COMBAT;
                                combat[0].SetCombat(players[0], players[1]);
                                break;
                            case GAME_PHASE.COMBAT:
                                foreach (Player p in players)
                                {
                                    p.Reset();
                                }
                                current_Phase = GAME_PHASE.PREPARATION;
                                round++;
                                break;
                            case GAME_PHASE.ITEM:
                                current_Phase = GAME_PHASE.PREPARATION;
                                break;
                        }
                        phase_Timer = 0;
                    }
                    break;
                case GAME_PHASE.ITEM: //Item phase
                    if (phase_Timer >= ITEM_TIME)
                    {
                        PhaseChange();
                    }
                    break;
            }
            phase_Timer += Time.deltaTime;
        }

        //Change the phase of the game
        //and reset the timer
        private void PhaseChange()
        {
            Debug.Log(current_Phase);
            previous_Phase = current_Phase;
            current_Phase = GAME_PHASE.CHANGE;
            phase_Timer = 0;
            
        }


        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(player_Prefab, startPos.position, startPos.rotation)
                : Instantiate(player_Prefab);

            NetworkServer.AddPlayerForConnection(conn, player);
            players.Add(player.GetComponent<Player>());
            if(players.Count >= 2)
            {
                for(short i = 0; i < players.Count; i++)
                {
                    players[i].readyToSetup = true;
                }
            }
        }
    }
}