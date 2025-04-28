using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WebSocketSharp;

public abstract class AbstractAvatarWithEyesAndVoice : AbstractAvatar
{
    protected GameObject avatarBody;
    protected GameObject avatarEyes;
    protected TextMeshProUGUI baloonText;

    protected override void Awake()
    {
        base.Awake();
        if (Application.IsPlaying(gameObject))
        {
            initializeAvatarWithEyes();
        }
    }
    protected void initializeAvatarWithEyes()
    {
        // Retrieve avatar parts
        avatarBody = transform.Find("Body").gameObject;
        avatarEyes = transform.Find("anchorVisionCone").gameObject;

        // Find the TextMeshPro component in the children of the avatar
        nameTextMeshPro = transform.Find("avatarName").GetComponent<TextMeshPro>();
        nameTextMeshPro.text = name;

        // Initiating the baloon
        baloonText = gameObject.transform.Find("Canvas/BaloonBg/BaloonTxt").GetComponent<TextMeshProUGUI>();
        baloonText.text = "start";
    }

    public void SetBaloonText(string message)
    {
        baloonText.text = message;
    }

    public void EnableDisableVisionCone(bool isActive)
    {
        avatarEyes.SetActive(isActive);
    }

    // Unity avatar receives message from jacamo agent
    protected override void OnMessage(object sender, MessageEventArgs e)
    {
        string data = e.Data;
        print("Received message: " + data);
        WsMessage message = null;
        try
        {
            message = JsonConvert.DeserializeObject<WsMessage>(data);
            switch (message.Type)
            {
                case "wsInitialization":
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        print("Connection established for " + objInUse.name);
                    });
                    break;
                case "walk":
                    print("Agent needs to reach destination.");
                    // Avatar receives the type of artifact to reach
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        WalkData data = message.Data.ToObject<WalkData>();
                        SetBaloonText("New destination: " + data.Target );
                        reachDestination( data.Target );
                    });
                    break;
                default:
                    print("Unknown message type for " + objInUse.name);
                    break;
            }
        }
        catch (Exception ex)
        {
            print("Error: " + ex.Message);
            print("Message could not be converted.");
            return;
        }
    }
}