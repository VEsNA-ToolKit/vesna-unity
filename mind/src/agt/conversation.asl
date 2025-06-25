
// // @evaluate_if_friend_true // let's start a conversation
// // +!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & not talking_to(_) <-
// //     .print(["I've found a friend"]);
// //     // First stop the agent
// //     // // !define_payload("stopAgent", AgentElement, ReturnMsg);
// //     // // !sendMessageToUnity(ReturnMsg);
// //     vesna.stop;
// //     vesna.rotate( AgentElement );
// //     +met_new_friend(AgentElement).
// // 
// // @already_talking_with_another_friend // agent is already talking with another friend
// // +!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & talking_to(Friend) & AgentElement \== Friend <-
// //     .print(["I've found a friend but i'm already talking with another friend"]).
// // 
// // @already_talking_with_friend // agent is already talking with the agent
// // +!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & talking_to(AgentElement) <-
// //     .print(["I'm already talking with ", AgentElement]).
// // 
// // @evaluate_if_friend_false // do not start a conversation
// // +!evaluateIfFriend(_, IsFriend) : IsFriend == false <-
// //     print(["I don't know him"]).

+!evaluateIfFriend( Ag )
    :   is_friend( Ag ) & not talking_to( _ )
    <-  .print( "It is a friend " );
        vesna.stop;
        vesna.rotate( Ag );
        +met_new_friend( Ag ).

+!evaluateIfFriend( Ag )
    :   is_friend( Ag ) & talking_to( Ag )
    <-  .print( "I'm already talking to ", Ag ).

// Added acquaintances plan
+!evaluateIfFriend( Ag )
    :   is_neutral( Ag ) & not talking_to( _ )
    <-  .print( "It is a neutral " );
        vesna.stop;
        vesna.rotate( Ag );
        +met_new_friend( Ag ).

+!evaluateIfFriend( Ag1 )
    :   is_friend( Ag1 ) & talking_to( Ag2 )
    <-  .print( "I'm already talking to ", Ag2, ", sorry ", Ag1 ).

+!evaluateIfFriend( Ag )
    <-  .print( "It is not a friend" ).
