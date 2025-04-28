/* INCLUDE */
{ include("libraryPlans.asl") }
{ include("buyItemPlans.asl") }
{ include("initialGoalsPlan.asl") }
{ include( "artifacts.asl")}

/* ----------------- INITIAL BELIEFS ---------------------*/
sells("FruitShop", [apple, banana, orange, mango, pineapple, strawberry, grape, pear, peach, watermelon]).
sells("DressShop", [shirt, pants, jacket, shoes, hat, dress, skirt, shorts, sweater]).
// Agent at startup is outside the supermarket
inside_supermarket(false).
// Agent at startup has not chosen the shop
actual_shop(none).

is_friend( Name ) :- friends( Friends ) & .member( Name, Friends ).

/* ----------------- LOGIC PLANS ---------------------*/

// Agent reached supermarket door - Inside supermarket
+!do_shopping
    :   shoppingList(Items) & Items \== [] 
    <-  -+inside_supermarket(true);
        !process_items(Items).

@process_items_finished
+!process_items([]) 
    <-  MsgList = ["All items have been processed. Exit from supermarket"];
        !writeLog(MsgList);
        !reach_dest("exitDoor");
        -movement_in_progress(Dest).

@process_items_recursive
+!process_items([FirstItem | Rest]) 
    : is_door_open(true) 
    <-  !retrieve_shop_type(ShopType, FirstItem);
        // Get shop names given the shop type
        !retrieve_nearest_artifacts_by_type(ShopType, Artifacts);
        Artifacts = [FirstShop | RestShop];
        !writeLog(["Let's reach ", FirstShop]);
        // reach shop and buy
        !reach_shop_and_buy(FirstShop, FirstItem);
        // Update shopping list
        -+shoppingList(Rest);
        !writeLog(["Proceed to buy the next item"]);
        !process_items(Rest). // recursive call

@reach_shop_and_buy[movement]
+!reach_shop_and_buy(Shop, item(ItemName, Quantity)) 
    :   actual_shop(A) & A \== Shop 
    <-  !reach_dest(Shop);
        -+shop_selected(true);
        -+actual_shop(Shop);
        lookupArtifact(Shop, ArtifactId);
        focus(ArtifactId);
        .wait(1000);
        !joinQueue(Shop, ArtifactId);
        .wait("+can_proceed"); // wait for my turn
        !check_item_and_buy(Shop, ItemName, Quantity); // buy item
        stopFocus(ArtifactId);
        -can_proceed;
        -+your_turn(false);
        -movement_in_progress(_);
        // Keep moving random again
        !start_walking.

@reach_shop_and_buy_already_in_the_shop[movement]
+!reach_shop_and_buy(Shop, item(ItemName, Quantity)) 
    : actual_shop(Shop) 
    <-  !writeLog(["Agent already inside the shop ", Shop]);
        lookupArtifact(Shop, ArtifactId);
        focus(ArtifactId);
        !writeLog(["Focus done"]);
        .wait(1000);
        // Put into the queue
        !joinQueue(Shop, ArtifactId);
        .wait("+can_proceed");
        !check_item_and_buy(Shop, ItemName, Quantity);
        stopFocus(ArtifactId);
        -can_proceed;
        -+your_turn(false).

@check_and_buy[atomic] // At this point i just have to buy the item. I can't be interrupted
+!check_item_and_buy(ShopName, ItemName, Quantity) <-
    !writeLog(["I want to buy ", Quantity, " ", ItemName]);
    !retrieve_shop_artifact_by_shop_name(ShopName, ArtifactId);
    .my_name(AgName);
    getItemInfo(message(AgName, ItemName), ItemInfoResponse)[artifact_id(ArtifactId)];
    ItemInfoResponse = iteminfo(Name, Price, ShopQt);
    !writeLog(["Response is: ", Name, Price, ShopQt]);
    !buy_item(ItemName, Quantity, Price, ShopQt, ArtifactId);
    ?budget(B);
    !writeLog(["New budget available: ", B]);
    -+shop_selected(false).

// Retrieve the SHOP TYPE where the agent can buy the item
@retrieve_shop_type
+!retrieve_shop_type(Shop, item(ItemName, Quantity)) : sells(Shop, Items) & .member(ItemName, Items) <-
    MsgList = ["The item ", ItemName, " can be bought to the ", Shop];
    !writeLog(MsgList).

@join_queue
+!joinQueue(Shop, ShopArtifactId) <-
    .my_name(Me);
    joinQueue(Me)[artifact_id(ArtifactId)].

@your_turn_event
+your_turn(true) : true <-
    !writeLog(["It's finally my turn "]);
    +can_proceed.


// NEW PLANS

+reached(friend, Friend) <-
    Content = "Hi! How are you?";
    !writeLog(["Sending to ", Friend, ": ", Content]);
    !update_balloon_message(Content);
    .wait(2000);
    .send(Friend, achieve, friend_message(Content)).

+new_agent_seen(AgentElement) <-
    // ?friends(FriendList);
    // checkIfAgentIsAFriend(AgentElement, FriendList, IsFriend);
    // .eval( IsFriend, is_friend( AgentElement ) );
    !evaluateIfFriend(AgentElement);
    -new_agent_seen(AgentElement).

+met_new_friend(AgentElement) <-
    !startConversation(AgentElement);
    -met_new_friend.

{ include("$jacamo/templates/common-cartago.asl") }
{ include("$jacamo/templates/common-moise.asl") }

// uncomment the include below to have an agent compliant with its organisation
//{ include("$moise/asl/org-obedient.asl") }
