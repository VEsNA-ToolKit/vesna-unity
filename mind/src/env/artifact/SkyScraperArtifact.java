package artifact;

import artifact.lib.maselements.ShopAbstractMasElementArtifact;

public class SkyScraperArtifact extends ShopAbstractMasElementArtifact {

    public void init(String artifactName, int webSocketPort, String properties) {
        // Initialize websocket connection and log
        super.init(artifactName, webSocketPort);
//        List<ItemInfoModel> fruitInfoModelList = ObjectMapperUtils.convertJsonStringToObject(properties,
//                new TypeReference<>() {
//                });
//        initializeProperty("fruitList", fruitInfoModelList);
    }
}
