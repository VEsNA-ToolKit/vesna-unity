using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractArtifact : AbstractMasElement
{
    // Properties in JSON format to configure .jcm file
    protected string artifactProperties;
    protected ArtifactTypeEnum artifactType;
    // List of all property names
    protected List<string> propertyNames = new List<string>();
    public string ArtifactProperties
    {
        get { return artifactProperties; }
        set { artifactProperties = value; }
    }

    public ArtifactTypeEnum ArtifactType
    {
        get { return artifactType; }
    }

    public List<string> PropertyNames
    {
        get { return propertyNames; }
    }
}