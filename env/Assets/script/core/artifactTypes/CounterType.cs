using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.Counter;
    }
}
