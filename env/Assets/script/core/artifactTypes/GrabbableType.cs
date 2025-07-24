using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.Grabbable;
    }
}
