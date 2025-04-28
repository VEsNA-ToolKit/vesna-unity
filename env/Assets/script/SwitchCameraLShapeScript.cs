using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCameraScript : MonoBehaviour
{
    public GameObject MainCamera;
    public GameObject FirstConversationCamera;
    public GameObject OtherConversationCamera;    


    void Update()
    {
        if (Input.GetKeyDown("0"))
        {
            ActiveMainCamera();
        }

        if (Input.GetKeyDown("1"))
        {
            CameraOne();
        }

        if (Input.GetKeyDown("2"))
        {
            CameraTwo();
        }
    }

    private void ActiveMainCamera()
    {
        MainCamera.SetActive(true);
        FirstConversationCamera.SetActive(false);
        OtherConversationCamera.SetActive(false);
    }

    void CameraOne()
    {
        MainCamera.SetActive(false);
        FirstConversationCamera.SetActive(true);
        OtherConversationCamera.SetActive(false);        
    }

    void CameraTwo()
    {
        MainCamera.SetActive(false);
        OtherConversationCamera.SetActive(true);
        FirstConversationCamera.SetActive(false);
    }

}