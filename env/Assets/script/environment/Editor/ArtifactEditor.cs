using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using script.core.util;
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
        // _root = new VisualElement();
        // // Add UI builder into the root to update the UI of the inspector
        // visualTree.CloneTree(_root);
        //
        // var artType = _artifactType.ToString();
        // artType = char.ToLower(artType[0]) + artType.Substring(1);
        // Debug.Log("Art Type: " + artType);
        // ShowAndHide(artType + "Properties");
        //
        // return _root;
        return null;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var properties = _artifactScript.Properties;

        if (properties == null || properties.Count == 0)
        {
            EditorGUILayout.HelpBox("No properties found for this artifact type.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space(10);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        EditorGUILayout.LabelField("Artifact Properties", headerStyle);
        EditorGUILayout.Space(6);

        // Collect changes to apply after iteration
        var pendingChanges = new Dictionary<string, object>();

        foreach (var (key, propertyValue) in properties)
        {
            var propertyType = ResolvePropertyType(propertyValue);
            var propertyName = ToDisplayLabel(key);

            switch (propertyType)
            {
                case "boolean":
                    var newBool = EditorGUILayout.Toggle(propertyName, propertyValue is true);
                    if ((bool)propertyValue != newBool)
                        pendingChanges[key] = newBool;
                    break;
                case "int":
                    var newInt = EditorGUILayout.IntField(propertyName, propertyValue is int i ? i : 0);
                    if ((int)propertyValue != newInt)
                        pendingChanges[key] = newInt;
                    break;
                case "float":
                    var newFloat = EditorGUILayout.FloatField(propertyName, propertyValue is float f ? f : 0f);
                    if (!Mathf.Approximately((float)propertyValue, newFloat))
                        pendingChanges[key] = newFloat;
                    break;
                case "list":
                    if (propertyValue is IList list)
                    {
                        var foldout = EditorGUILayout.BeginFoldoutHeaderGroup(true, propertyName);
                        if (foldout)
                        {
                            EditorGUI.indentLevel++;
                            
                            for (var j = 0; j < list.Count; j++)
                            {
                                var element = list[j];
                                if (element == null)
                                {
                                    EditorGUILayout.LabelField($"[{j}] (null)");
                                    continue;
                                }

                                var elementType = element.GetType();
                                EditorGUILayout.LabelField($"[{j}] {elementType.Name}", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;

                                foreach (var field in elementType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                                {
                                    var fieldValue = field.GetValue(element);
                                    var fieldType = field.FieldType;

                                    if (fieldType == typeof(string))
                                        field.SetValue(element, EditorGUILayout.TextField(ToDisplayLabel(field.Name), fieldValue as string));
                                    else if (fieldType == typeof(int))
                                        field.SetValue(element, EditorGUILayout.IntField(ToDisplayLabel(field.Name), (int)(fieldValue ?? 0)));
                                    else if (fieldType == typeof(float) || fieldType == typeof(double))
                                        field.SetValue(element, EditorGUILayout.FloatField(ToDisplayLabel(field.Name), Convert.ToSingle(fieldValue ?? 0f)));
                                    else if (fieldType == typeof(bool))
                                        field.SetValue(element, EditorGUILayout.Toggle(ToDisplayLabel(field.Name), (bool)(fieldValue ?? false)));
                                    else if (fieldType.IsEnum)
                                        field.SetValue(element, EditorGUILayout.EnumPopup(ToDisplayLabel(field.Name), (Enum)fieldValue));
                                    else
                                        EditorGUILayout.LabelField($"{field.Name}: {fieldValue}");
                                }

                                EditorGUI.indentLevel--;
                                EditorGUILayout.Space(4);

                                if (GUILayout.Button($"Remove {elementType.Name}"))
                                {
                                    list.RemoveAt(j);
                                    break;
                                }

                                EditorGUILayout.Space(6);
                            }

                            if (GUILayout.Button($"Add new {list.GetType().GetGenericArguments()[0].Name}"))
                            {
                                var elementType = list.GetType().GetGenericArguments()[0];
                                list.Add(Activator.CreateInstance(elementType));
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                    break;
                default:
                    EditorGUILayout.TextField(propertyName, propertyValue?.ToString() ?? "");
                    break;
            }
        }

        // Apply collected changes after iteration
        foreach (var (key, value) in pendingChanges)
        {
            properties[key] = value;
        }
    }
    private void ShowAndHide(string propertyName)
    {
        _propertyNames = _artifactScript.PropertyNames;        
        if (_propertyNames == null) return;
        
        _property = _root.Q<PropertyField>(propertyName);
        if (_property != null)
        {
            Debug.Log("Showing " + propertyName);
            _property.style.display = DisplayStyle.Flex;
        }
        
        // Hide others        
        foreach(var propName in _propertyNames)
        {
            if (propName == propertyName) continue;
            
            // Skip snapToSurface - handled in SnapPointArtifact
            if (propName == "snapToSurface") continue;
            
            var propField = _root.Q<PropertyField>(propName);
            if (propField == null) continue;
            
            // Hide artifactType if this is a SnapPoint
            if (_artifactType == ArtifactTypeEnum.SnapPoint && propName == "artifactType") continue;
            
            Debug.Log("Hiding " + propName);
            propField.style.display = DisplayStyle.None;
        }
    }
    
    private static string ResolvePropertyType(object propertyValue)
    {
        var propertyType = "string";
        if (propertyValue != null)
        {
            var type = propertyValue.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                propertyType = "list";
            }
            else
            {
                propertyType = type.Name.ToLower();
            }
        }

        return propertyType;
    }
    
    private static string ToDisplayLabel(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "";
        System.Text.StringBuilder sb = new();
        sb.Append(char.ToUpper(rawName[0]));

        for (var i = 1; i < rawName.Length; i++)
        {
            if (char.IsUpper(rawName[i]) && rawName[i - 1] != ' ')
                sb.Append(' ');
            sb.Append(rawName[i]);
        }

        return sb.ToString();
    }
}
