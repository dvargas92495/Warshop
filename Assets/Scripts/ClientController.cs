﻿using System;   
using Z8.Generic;
//using Lidgren.Network;
using UnityEngine;

public class ClientController {

    //private NetClient client;

    public ClientInializationObject Initialize () {
        //var config = new NetPeerConfiguration("Z8Server");
        //client = new NetClient(config);
        //client.Start();
        //NetConnection nc = client.Connect(host: GameConstants.SERVER_IP, port: 12345);
        return new ClientInializationObject();
    }

    public void SubmitTurn (string message) {
        Debug.Log("submitted");
        //var messageObj = client.CreateMessage();
        //messageObj.Write(message);
        //client.SendMessage(messageObj, NetDeliveryMethod.ReliableOrdered);
        Debug.Log("sent");
    }
}
