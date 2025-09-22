using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractArtifact : AbstractMasElement
{
    // Properties in JSON format to configure .jcm file
    protected string artifactProperties;
    [SerializeField]
    protected ArtifactTypeEnum artifactType;
    [SerializeField] public bool isGrabbable;
    // List of all property names
    protected List<string> propertyNames = new List<string>();
    public string ArtifactProperties
    {
        get => artifactProperties;
        protected set => artifactProperties = value;
    }

    public ArtifactTypeEnum ArtifactType => artifactType;

    public List<string> PropertyNames => propertyNames;
}