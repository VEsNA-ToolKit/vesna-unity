package artifact;

import artifact.lib.maselements.ShopAbstractMasElementArtifact;
import artifact.lib.model.item.ItemInfoModel;
import artifact.lib.utils.ObjectMapperUtils;
import com.fasterxml.jackson.core.type.TypeReference;

import java.util.List;

public class BarArtifact extends ShopAbstractMasElementArtifact {

    public void init(String artifactName, int webSocketPort, String properties) {
        // Initialize websocket connection and log
        super.init(artifactName, webSocketPort, "coffeeList");
        List<ItemInfoModel> coffeeList = ObjectMapperUtils.convertJsonStringToObject(properties,
                new TypeReference<>() {
                });
        writeLog("Properties: " + coffeeList.toString());
        initializeProperty("coffeeList", coffeeList);
    }
}
