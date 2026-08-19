using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObjectType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.RoomObject;
    }
}