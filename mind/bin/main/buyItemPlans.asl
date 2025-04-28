// Plan to buy item by calling shop artifact
@buy_item
+!buy_item(ItemName, Quantity, Price, ShopQt, ArtifactId) : budget(B) & (B - (Quantity * Price) >= 0) & Quantity <= ShopQt <-
    .my_name(AgName);
    buyItem(message(AgName, ItemName, Quantity))[artifact_id(ArtifactId)];
    !writeLog(["Bought ", Quantity, " ", ItemName]);
    dequeue(AgName)[artifact_id(ArtifactId)];
    //Update budget
    TotalSpent = (Quantity * Price);
    NewBudget = B - TotalSpent;
    -+budget(NewBudget).

@buy_item_no_enough_money
+!buy_item(ItemName, Quantity, Price, ShopQt, ArtifactId) : budget(B) & (B - (Quantity * Price) < 0) <-
    .my_name(Name);
    dequeue(Name)[artifact_id(ArtifactId)];
    !writeLog(["Agent hasn't enough money to buy ", ItemName]).

// Quantity > ShopQuantity -> buy the maximum available in the shop
@buy_item_no_enough_quantity
+!buy_item(ItemName, Quantity, Price, ShopQt, ArtifactId) : budget(B) & Quantity > ShopQt & ShopQt \== 0 <-
    !writeLog(["The shop hasn't the quantity requested. Proceed to buy just the max available."]);
    .my_name(AgName);
    buyItem(message(AgName, ItemName, ShopQt))[artifact_id(ArtifactId)];
    !writeLog(["Bought ", ShopQt, " ", ItemName]);
    dequeue(AgName)[artifact_id(ArtifactId)];
    TotalSpent = (ShopQt * Price);
    NewBudget = B - TotalSpent;
    -+budget(NewBudget).

@buy_item_item_finished
+!buy_item(ItemName, Quantity, Price, ShopQt, ArtifactId) : ShopQt == 0 <-
    .my_name(Name);
    dequeue(Name)[artifact_id(ArtifactId)];
    !writeLog(["The shop has finished the item."]).

@retrieve_shop_artifact
+!retrieve_shop_artifact_by_shop_name(ShopName, ArtifactId) <-
    lookupArtifact(ShopName, ArtifactId).