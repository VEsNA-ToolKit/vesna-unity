package artifact;

import artifact.lib.maselements.ShopAbstractMasElementArtifact;
import artifact.lib.model.item.ItemInfoModel;
import artifact.lib.utils.ObjectMapperUtils;
import com.fasterxml.jackson.core.type.TypeReference;
import java.util.List;

public class DressShopArtifact extends ShopAbstractMasElementArtifact {

    public void init(String artifactName, int webSocketPort, String properties) {
        super.init(artifactName, webSocketPort, "clothesList");
        List<ItemInfoModel> dressInfoModelList = ObjectMapperUtils.convertJsonStringToObject(properties,
                new TypeReference<>() {
                });
        writeLog("Properties: " + dressInfoModelList.toString());
        initializeProperty("clothesList", dressInfoModelList);
    }

}
