using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactInfo
{
    private string artifactName;
    private string artifactType;    

    public string ArtifactName
    {
        get { return this.artifactName; }
        set { this.artifactName = value; }
    }

    public string ArtifactType
    {
        get { return this.artifactType; }
        set { this.artifactType = value; }
    }
}
