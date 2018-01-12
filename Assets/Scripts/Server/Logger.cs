using System;
using UnityEngine;

class Logger {

    public static void ServerLog(object value)
    {
        if (Application.isEditor)
        {
            Debug.Log("SERVER: " + value);
        }
        else
        {
            Console.WriteLine(value);
        }
    }
    
    public static void ClientLog(object value)
    {
        Debug.Log("CLIENT: " + value);
    }
}
