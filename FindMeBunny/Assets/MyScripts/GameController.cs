﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;


public class GameController : NetworkBehaviour {
    public static float counter = 10;
    public static GameObject[] PlayersList;
    public static bool SwitchOffLight = false;
    private static bool Called = false;
    //public Text DisplayText;
    // Use this for initialization
    void Start ()
    {
        Called = false;
        counter = 10;
        SwitchOffLight = false;
    }
    
    void Update()
    {
        RpcSwitchOffLights();
        RpcDisplayCounter();
        RpcSetLookingPlayer(GameObject.FindGameObjectsWithTag("Player").OrderBy(go => go.GetComponent<NetworkIdentity>().netId.Value).ToArray());
        //print(PlayersList.Length);
    }
    [Command] 
    void CmdSetLookingPlayer()
    {
        PlayersList = GameObject.FindGameObjectsWithTag("Player").OrderBy(go => go.GetComponent<NetworkIdentity>().netId).ToArray();
        RpcSetLookingPlayer(PlayersList);
        //Debug.Log("Server: " + PlayersList[0].GetComponent<NetworkIdentity>().netId + " : " + PlayersList[1].GetComponent<NetworkIdentity>().netId);
    }

    //[ClientRpc]
    void RpcSetLookingPlayer(GameObject [] List)
    {
        PlayersList = List;
        foreach(GameObject p in PlayersList) {
            Debug.Log("UnetId: " +p.GetComponent<NetworkIdentity>().netId);
        }

        if (PlayersList.Length > 0)
        {
            PlayersList[0].layer = LayerMask.NameToLayer("Ignore Raycast");
            PlayersList[0].GetComponent<PlayerControll>().isLooking = true;
        }
    }

    
    public  bool AllPlayersWereCatched()
    {
        PlayersList = GameObject.FindGameObjectsWithTag("Player");
        if (PlayersList.Length > 0)
        {      
            foreach (GameObject p in PlayersList)
            {
                if (p.GetComponent<PlayerControll>().isLooking == false)
                    return false;
            }
        }
        
        return true;
    }
    //Dodanie RPC skutkuje tym ze jest mniejsza roznica w counterach na klientach
    [RPC]
    void RpcSwitchOffLights()
    {
        if (counter > 0)
        {
            counter -= Time.deltaTime;
        }
        if (counter < 0 && SwitchOffLight == false)
        {
            GameObject.FindGameObjectWithTag("MainLight").SetActive(false);
            //GameObject.FindGameObjectWithTag("roof").SetActive(true);
            SwitchOffLight = true;
        }
    }
    [Command]
    void CmdReturnToLobby()
    {
        RpcReturnToLobby();
    }

    [RPC]
    public void RpcReturnToLobby()
    {
        StartCoroutine(ServerCountdownCoroutine());
        if (Called == false && isServer)
        {
            GameObject.FindGameObjectWithTag("Lobby").GetComponent<Prototype.NetworkLobby.LobbyManager>().SendReturnToLobby();
            Called = true;
        }
    }
            
    public IEnumerator ServerCountdownCoroutine()
    {
        float remainingTime = 5.0f;
        int floorTime = Mathf.FloorToInt(remainingTime);

        while (remainingTime > 0)
        {
            yield return null;

            remainingTime -= Time.deltaTime;
            int newFloorTime = Mathf.FloorToInt(remainingTime);

            if (newFloorTime != floorTime)
            {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                floorTime = newFloorTime;

                for (int i = 0; i < PlayersList.Length; ++i)
                {
                    if (PlayersList[i] != null)
                    {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                        //(PlayersList[i] as GameObject).RpcDisplay
                    }
                }
            }
        }
        
    }
    
    [RPC]
    void RpcDisplayCounter()
    {
        
        if (AllPlayersWereCatched() && counter < 0)
        {
            CmdReturnToLobby();
        }

        if (PlayersList.Length != 0 && Called == false)
            foreach (GameObject p in PlayersList)
            {
                if (counter > 0)
                {
                    p.GetComponent<PlayerControll>().DisplayText.enabled = true;
                    p.GetComponent<PlayerControll>().DisplayText.text = "time to hide: " + counter.ToString();
                }
                else p.GetComponent<PlayerControll>().DisplayText.enabled = false;

            }
    }//EndDisplayCounter

    private static int SortByName(GameObject o1, GameObject o2)
    {
        return o1.name.CompareTo(o2.name);
    }
}
