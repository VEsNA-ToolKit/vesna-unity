{ include( "conversation_data.asl" ) }

@agent_not_busy_start_conversation
+!stop_and_talk[source(Friend)]
    : not busy <- 
    -+actual_intention(talk);
    vesna.walk(Friend).

+!start_conversation( Friend )
    <-  .send(Friend, askAll, talking_to(_), A);
        for( .member(T, A ) ) {
            +T;
        }
        !join_conversation( Friend );
        !talk("Hello").

+!join_conversation( Friend )
    :   not talking_to( Other )[ source( Friend ) ]
    <-  vesna.stop;
        .my_name(Me);
        +conversation([Me, Friend]);
        .send(Friend, signal, conversation_update( [Me, Friend]));
        +talking_to(Friend);
        -+actual_intention(talk);
        .print("Starting conversation with: ", Friend );
        vesna.says(Friend, "Heiiii!", "happy");
        .send(Friend, achieve, stop_and_talk).

+!join_conversation(Friend) 
    :   .count( talking_to( _ )[source( Friend)], N ) & N > 1
    <-  .print("Conversation full (", N, " agents), cannot join");
        !walk_and_not_talk.

+!join_conversation( Friend )
    <-  vesna.walk(Friend);
        .my_name(Me);
        .send( Friend, askOne, conversation( _ ), conversation( AgList ));
        .union(AgList, [Me], NewAgList);
        +conversation(NewAgList);
        for( .member( Ag, AgList ) ) {
            +talking_to( Ag );
        }
        -+actual_intention(talk);
        .send(AgList, signal, conversation_update(NewAgList));
        .print("I successfully joined conversation with ", Friend, " we are ", NewAgList ).

+!talk( Msg )
    :   conversation( AgList )
    <-  .wait(10000);
        .my_name( Me );
        .delete( Me, AgList, Others );
        .print("Others in TALK: ", Others);
        .send( Others, signal, msg( Msg ) );
        .print( "CONVERSATION ", Msg ).

-!talk( _ )
    <-  .print( "You cannot talk if you are not inside a conversation..." ).


+!walk_and_not_talk
    <-   .findall(talking_to(_)[source(_)], talking_to(_)[source(_)], L);
        for(.member(talking_to(X)[source(Y)], L)) {
            -talking_to(X)[source(Y)];
        }
        .wait(2000);          
        .print("CAMMINA");
        -+actual_intention(start_walking);
        !start_walking.

+conversation_update( AgList )[source(Sender)]
    :   conversation( _ ) 
    <-  -+conversation( AgList );
        +talking_to( Sender );
        .print("Conversation aggiornata: ", AgList ).

+conversation_update( AgList )[source(Sender)] 
    <-  +conversation( AgList );
        .my_name( Me );
        .delete( Me, AgList, AgOthers);
        .print( "Conversazione creata", AgList );
        for (.member( Ag, AgOthers ) ) {
            +talking_to( Ag );
        }.

// +msg( "Bye" )[source( Friend)]
//     :   conversation( AgList )
//     <-  !exit_conversation.

+msg( Content )[source(Friend)]
    :   conversation( AgList ) & ans( Content, _ , Answer ) /*& mood(Mood)& ans( Content, Mood, Answer )*/ 
    <-  vesna.says( Answer );
        .print( "CONVERSATION (Answer to ", Friend, ") ", Answer );
        .my_name( Me );
        .delete(Me, AgList, Others);
        if (Answer == "Bye") {
            .send( self, signal, exit_conversation);
        } else {
            !talk( Answer );  
        };
        .send(Others, signal, msg( Answer ) ).

@[atomic]
+exit_conversation
    :   conversation( AgList )
    <-  .print( "Scambio le informazioni" );
        -conversation(_);
        .findall( talking_to(_)[source(_)], talking_to(_)[source(_)], L );
        for( .member(talking_to(X)[source(Y)], L ) ) {
            -talking_to(X)[source(Y)];
        }
        .my_name( Me );
        .delete( Me, AgList, Others );
        .findall(seen_artifact(_, _), seen_artifact(_, _), Artifacts);
        .send( Others, tell, Artifacts );
        .send( Others, signal, leaving);
        -+actual_intention( start_walking );
        !start_walking.

@[atomic]
+leaving[souce(Ag)]
    :   conversation( AgList )
    <-  .delete( Ag, AgList, NewAgList );
        -+conversation( NewAgList ).
