using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class ChatUI : NetworkBehaviour
{
    const ulong SYSTEM_ID = 999999999;
    public ulong Host_ID;
    public TMPro.TMP_Text txtChatLog;
    public Button btnSend;
    public TMPro.TMP_InputField inputMessage;

    ulong[] singleClientId = new ulong[1];

    public void Start()
    {
        btnSend.onClick.AddListener(ClientOnSendClicked);
        inputMessage.onSubmit.AddListener(ClientOnInputSubmit);
    }

    public override void OnNetworkSpawn()
    {
        txtChatLog.text = "--Start Chat Log--";

        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HostOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HostOnClientDisconnected;
            Host_ID = NetworkManager.Singleton.LocalClientId;
            DisplayMessageLocally("You are the host", SYSTEM_ID);
        }
        else
        {
            DisplayMessageLocally($"You are PLayer #{NetworkManager.Singleton.LocalClientId}!", SYSTEM_ID);
        }
    }

    private void SendUIMessage()
    {
        string msg = inputMessage.text;
        inputMessage.text = "";
        SendChatMessageServerRpc(msg);
    }
    private void SendDirectMessage(string message, ulong from, ulong to)
    {
        ClientRpcParams rpcParams = default;
        rpcParams.Send.TargetClientIds = singleClientId;

        singleClientId[0] = from;
        SendChatMessageClientRpc($"<whisper> {message}", from , rpcParams);

        singleClientId[0] = to;
        SendChatMessageClientRpc($"<whisper> {message}", from , rpcParams);
    }



    //-----------------------------------
    // Events
    //-----------------------------------

    private void HostOnClientConnected(ulong clientId)
    {
        SendChatMessageClientRpc($"Client {clientId} connected", SYSTEM_ID);
    }

    private void HostOnClientDisconnected(ulong clientId)
    {
        SendChatMessageClientRpc($"Client {clientId} disconnected",SYSTEM_ID);
    }

    public void ClientOnSendClicked()
    {
        SendUIMessage();
    }

    public void ClientOnInputSubmit(string text)
    {
        SendUIMessage();
    }


    //-----------------------------------
    // RPC
    //-----------------------------------

    [ClientRpc]
    public void SendChatMessageClientRpc(string message, ulong from, ClientRpcParams clientRpcParams = default)
    {
        DisplayMessageLocally(message, from);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Host got message: {message}");

        if (message.StartsWith("@"))
        {
            string[] parts = message.Split(" ");
            string clientIdStr = parts[0].Replace("@", "");
            ulong toClientId = ulong.Parse(clientIdStr);

            SendDirectMessage(message, serverRpcParams.Receive.SenderClientId, toClientId);
        }
        else
        {
            SendChatMessageClientRpc(message, serverRpcParams.Receive.SenderClientId);
        }
    }

    public void DisplayMessageLocally(string message, ulong from)
    {
        Debug.Log(message);

        string who;

        if(from == NetworkManager.Singleton.LocalClientId)
        {
            who = "you";
        } 
        else if (from == SYSTEM_ID)
        {
            who = "system";
        }
        else if (from == Host_ID)
        {
            who = "Host";
        }
        else
        {
            who = $"Player #{from}";
        }
        string newMessage = $"\n[{who}]: {message}";
        txtChatLog.text += newMessage;
    }

}
