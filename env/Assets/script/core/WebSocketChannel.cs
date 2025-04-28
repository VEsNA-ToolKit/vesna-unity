// using System;
// using WebSocketSharp;
// using UnityEngine;
// using System.Threading.Tasks;
// using Unity.VisualScripting.FullSerializer;
//
// public class WebSocketChannel
// {
//
//     private WebSocket ws;
//     private WSConnectionInfoModel connectionInfo;
//     public WSConnectionInfoModel ConnectionInfoModel { get { return connectionInfo; } }
//     private bool isWebSocketConnected = false;
//
//     public WebSocketChannel(WSConnectionInfoModel connectionInfo, System.EventHandler<WebSocketSharp.MessageEventArgs> onMessage)
//     {
//         ws = new WebSocket(connectionInfo.getUrl());
//         ws.OnOpen += OnOpen;
//         ws.OnMessage += onMessage;
//         ws.OnClose += OnClose;
//         ws.OnError += OnError;
//         this.connectionInfo = connectionInfo;
//     }
//
//     public bool IsWebSocketConnected
//     {
//         get
//         {
//             // Return the value of the private field
//             return isWebSocketConnected;
//         }
//         set
//         {
//             // Set the value of the private field
//             isWebSocketConnected = value;
//         }
//     }
//
//     public void connect()
//     {
//         // Connect to the WebSocket server
//         ws.Connect();
//     }
//
//     public async void sendMessage(string message)
//     {
//         Debug.Log("Sending message " + message);
//         while (!IsWebSocketConnected)
//         {
//             await Task.Delay(2000);
//         }
//         ws.Send(message);
//     }
//
//     private void OnOpen(object sender, System.EventArgs e)
//     {
//         Debug.Log("WebSocket connection opened successfully for: " + connectionInfo.getName());
//         IsWebSocketConnected = true;
//     }
//
//     private async void OnClose(object sender, CloseEventArgs e)
//     {
//         Debug.Log("WebSocket connection closed with reason: " + e + " and code: " + e.Code);
//         // Check if connection is closed
//         if (e.Code == 1006)
//         {
//             await Task.Delay(5000);
//             connect();
//         }
//     }
//
//     private void OnError(object sender, ErrorEventArgs e)
//     {
//         Debug.LogError("WebSocket error: " + e.Message);
//     }
//
// }
using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;

public class WebSocketChannel
{
    private WebSocketServer wss;
    private bool isServerRunning = false;
    private WSConnectionInfoModel connectionInfo;
    public WSConnectionInfoModel ConnectionInfoModel => connectionInfo;

    private EventHandler<MessageEventArgs> onMessageHandler;

    public WebSocketChannel(WSConnectionInfoModel connectionInfo)
    {
        this.connectionInfo = connectionInfo;
        string url = connectionInfo.getUrl(); // Es: "ws://localhost:8080"
        Uri uri = new Uri(url);
        wss = new WebSocketServer(uri.Port);
    }

    public WebSocketChannel( WSConnectionInfoModel connectionInfo, System.EventHandler<WebSocketSharp.MessageEventArgs> onMessage ) {
        this.connectionInfo = connectionInfo;
        this.onMessageHandler = onMessage;
        string url = connectionInfo.getUrl(); // Es: "ws://localhost:8080"
        Uri uri = new Uri(url);
        wss = new WebSocketServer(uri.Port);
    }

    public bool IsServerRunning
    {
        get { return isServerRunning; }
        private set { isServerRunning = value; }
    }

    public void StartServer()
    {
        wss.AddWebSocketService<CustomWebSocketBehavior>("/", () => new CustomWebSocketBehavior( onMessageHandler ) );
        wss.Start();
        IsServerRunning = true;
        Debug.Log($"WebSocket server started on port {wss.Port}");
    }

    public void StopServer()
    {
        if (IsServerRunning)
        {
            wss.Stop();
            IsServerRunning = false;
            Debug.Log("WebSocket server stopped.");
        }
    }

    // 🔸 Metodo per inviare messaggio a tutti i client connessi
    public void sendMessage(string message)
    {
        Debug.Log("Sending message to all clients: " + message);

        // Itera tra tutti i comportamenti (client connessi)
        wss.WebSocketServices["/"].Sessions.Broadcast(message);
    }

    public class CustomWebSocketBehavior : WebSocketBehavior
    {

    	private EventHandler<MessageEventArgs> onMessageHandler;

     	public CustomWebSocketBehavior( EventHandler<MessageEventArgs> onMessageHandler)
     	{
     		this.onMessageHandler = onMessageHandler;
     	}

        protected override void OnMessage(MessageEventArgs e)
        {
            Debug.Log("Server received message: " + e.Data);
            onMessageHandler?.Invoke( this, e );
        }

        protected override void OnOpen()
        {
            Debug.Log("Client connected.");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log($"Client disconnected with reason: {e.Reason}");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogError("WebSocket Server Error: " + e.Message);
        }
    }
}
