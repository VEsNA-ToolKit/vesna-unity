
@evaluate_if_friend_true // let's start a conversation
+!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & not talking_to(_) <-
    !writeLog(["I've found a friend"]);
    // First stop the agent
    // // !define_payload("stopAgent", AgentElement, ReturnMsg);
    // // !sendMessageToUnity(ReturnMsg);
    vesna.stop;
    vesna.rotate( AgentElement );
    +met_new_friend(AgentElement).

@already_talking_with_another_friend // agent is already talking with another friend
+!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & talking_to(Friend) & AgentElement \== Friend <-
    !writeLog(["I've found a friend but i'm already talking with another friend"]).

@already_talking_with_friend // agent is already talking with the agent
+!evaluateIfFriend(AgentElement, IsFriend) : IsFriend == true & talking_to(AgentElement) <-
    !writeLog(["I'm already talking with ", AgentElement]).

@evaluate_if_friend_false // do not start a conversation
+!evaluateIfFriend(_, IsFriend) : IsFriend == false <-
    !writeLog(["I don't know him"]).
