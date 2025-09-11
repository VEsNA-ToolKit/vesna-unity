package artifact.lib.maselements;

import artifact.lib.maselements.AbstractMasElementAgent;
import cartago.OPERATION;
import cartago.OpFeedbackParam;
import cartago.Artifact;

public class AgentMasElement extends Artifact {

    //@Override
    @OPERATION
    public void processConversation(String content, OpFeedbackParam<String> replyContent) {

        switch (content) {
            /*case "Hi! How are you?" -> //Message sent from the agent who reached his friend
                    replyContent.set("How long! I'm fine and you?");
            case "How long! I'm fine and you?" -> replyContent.set("I'm fine thanks. See you next time!");
            case "I'm fine thanks. See you next time!", "Bye!!" -> replyContent.set("Bye!!");*/
        case "Ciao, mi unisco anch'io!" ->
            replyContent.set("How long! I'm fine and you?");
            
        case "Hi! How are you?" -> 
            replyContent.set("How long! I'm fine and you?");
            
        case "How long! I'm fine and you?" -> 
            replyContent.set("I'm fine thanks. See you next time!");
            
        case "I'm fine thanks. See you next time!", "Bye!!" -> 
            replyContent.set("Bye!!");

            default -> replyContent.set("Error");
        }

    }
}
