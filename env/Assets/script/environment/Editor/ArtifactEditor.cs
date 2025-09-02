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
    private Artifact _artifactScript;
    // Artifact type
    private ArtifactTypeEnum _artifactType;
    private VisualElement _root;

    // All artifact properties
    private PropertyField _property;
    private List<string> _propertyNames;

    private void OnEnable()
    {
        _artifactScript = (Artifact)target;
        _artifactType = _artifactScript.ArtifactType;        
    }

    public override VisualElement CreateInspectorGUI()
    {
        _root = new VisualElement();
        // Add UI builder into the root to update the UI of the inspector
        visualTree.CloneTree(_root);

        string artType = _artifactType.ToString();
        artType = char.ToLower(artType[0]) + artType.Substring(1);
        Debug.Log("Art Type: " + artType);
        ShowAndHide(artType + "Properties");

        return _root;
    }

    private void ShowAndHide(string propertyName)
    {
        _propertyNames = _artifactScript.PropertyNames;        
        _property = _root.Q<PropertyField>(propertyName);
        if (_property != null)
        {
            Debug.Log("Showing " + propertyName);
            _property.style.display = DisplayStyle.Flex;
        }
    
        // Hide others        
        foreach(var propName in _propertyNames)
        {
            if (propName != propertyName)
            {
                var propField = _root.Q<PropertyField>(propName);
                if (propField != null)
                {
                    Debug.Log("Hiding " + propName);
                    propField.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
