package artifact;

import artifact.lib.maselements.ShopAbstractMasElementArtifact;
import artifact.lib.model.item.ItemInfoModel;
import artifact.lib.utils.ObjectMapperUtils;
import com.fasterxml.jackson.core.type.TypeReference;
import java.util.List;

public class FruitShopArtifact extends ShopAbstractMasElementArtifact {

    public void init(String artifactName, int webSocketPort, String properties) {
        // Initialize websocket connection and log
        super.init(artifactName, webSocketPort, "fruitList");
        List<ItemInfoModel> fruitInfoModelList = ObjectMapperUtils.convertJsonStringToObject(properties,
                new TypeReference<>() {
                });
        writeLog("Properties: " + fruitInfoModelList.toString());
        initializeProperty("fruitList", fruitInfoModelList);
    }
}
