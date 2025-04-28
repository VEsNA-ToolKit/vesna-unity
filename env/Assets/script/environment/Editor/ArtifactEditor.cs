using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


[CustomEditor(typeof(Artifact), true)]
public class ArtifactEditor : Editor
{

    public VisualTreeAsset visualTree;
    // Script that has dynamic inspector
    private Artifact artifactScript;
    // Artifact type
    private ArtifactTypeEnum artifactType;
    private VisualElement root;

    // All artifact properties
    private PropertyField property;
    private List<string> propertyNames;

    private void OnEnable()
    {
        artifactScript = (Artifact)target;
        artifactType = artifactScript.gameObject.GetComponent<GenericArtifactType>().GetShopType();        
    }

    public override VisualElement CreateInspectorGUI()
    {
        root = new VisualElement();
        // Add UI builder into the root to update the UI of the inspector
        visualTree.CloneTree(root);

        string artType = artifactType.ToString();
        artType = char.ToLower(artType[0]) + artType.Substring(1);
        Debug.Log("Art Type: " + artType);
        ShowAndHide(artType + "Properties");

        return root;
    }

    private void ShowAndHide(string propertyName)
    {
        propertyNames = artifactScript.PropertyNames;        
        property = root.Q<PropertyField>(propertyName);
        if (property != null)
        {
            Debug.Log("Showing " + propertyName);
            property.style.display = DisplayStyle.Flex;
        }
        // Hide others        
        foreach(string propName in propertyNames)
        {
            if (propName != propertyName)
            {
                Debug.Log("Hiding " + propName);
                root.Q<PropertyField>(propName).style.display = DisplayStyle.None;
            }
        }
    }
}
