using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DressShopType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.DressShop;
    }
}
