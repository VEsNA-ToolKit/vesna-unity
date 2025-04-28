using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.Door;
    }
}
