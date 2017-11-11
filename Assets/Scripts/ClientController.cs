using System;   
using Z8.Generic;
using Lidgren.Network;

public class ClientController {

    private NetClient client;

    public ClientInializationObject Initialize () {
        var config = new NetPeerConfiguration("Z8 Game");
        client = new NetClient(config);
        client.Start();
        client.Connect(host: GameConstants.SERVER_IP, port: 12345);
        return new ClientInializationObject();
    }

    public void SubmitTurn (string message) {
        var messageObj = client.CreateMessage();
        messageObj.Write(message);
        client.SendMessage(messageObj, NetDeliveryMethod.ReliableOrdered);
    }
}
