// Define an interface named IDamageable
using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public abstract class AbstractMasElement : MonoBehaviour
{
    public GameObject objInUse;
    public string port;
    protected WebSocketChannel wsChannel;
    public string Port => port;

    protected void initializeWebSocketConnection(System.EventHandler<WebSocketSharp.MessageEventArgs> OnMessage)
    {
        // Initialize new web socket connection
        string url = "ws://localhost:" + port;
        WSConnectionInfoModel wSConnectionInfoModel = new WSConnectionInfoModel(url, "AGENT", objInUse.name);
        wsChannel = new WebSocketChannel(wSConnectionInfoModel, OnMessage);
    }

    public async Task startServer() {
        await Task.Run( async () => wsChannel.StartServer() );
    }

    // Method to connect to the websocket channel
    // public async Task connectWs()
    // {
    //     int maxRetryAttempt = 3;
    //     await Task.Run(async () =>
    //     {
    //         int currentAttempt = 1;
    //         while (!wsChannel.IsWebSocketConnected && currentAttempt < maxRetryAttempt)
    //         {
    //             print("Connecting attemp n°: " + currentAttempt + " for " + wsChannel.ConnectionInfoModel.getName());
    //             wsChannel.connect();
    //             if (wsChannel.IsWebSocketConnected)
    //             {                    
    //                 break;
    //             }
    //             currentAttempt++;
    //             // Wait 5 seconds before retrying
    //             await Task.Delay(5000);
    //         }
    //     });
    // }

    // public bool testConnection()
    // {
    //     return wsChannel.IsWebSocketConnected;
    // }

    public string convertObjectIntoJson<T>(T objToConvert)
    {
        // Convert any object to JSON
        return JsonConvert.SerializeObject(objToConvert);
    }

    public string EscapeJson(string json)
    {
        // Escape double quotes and backslashes
        return json.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}