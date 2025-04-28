using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitShopType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.FruitShop;
    }
}
