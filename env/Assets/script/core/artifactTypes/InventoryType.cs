using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.Inventory;
    }
}
