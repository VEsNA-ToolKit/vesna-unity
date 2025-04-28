using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyScraperType : GenericArtifactType
{
    public override ArtifactTypeEnum GetShopType()
    {
        return ArtifactTypeEnum.SkyScraper;
    }
}
