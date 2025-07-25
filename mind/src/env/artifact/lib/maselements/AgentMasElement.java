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
        case "Hi! How are you?" -> 
            replyContent.set("How long! I'm fine and you?");
            
        case "How long! I'm fine and you?" -> 
            replyContent.set("I'm fine thanks. See you next time!");
            
        case "I'm fine thanks. See you next time!", "Bye!!" -> 
            replyContent.set("Bye!!");

        case "Hey, do you remember our last trip?" -> 
            replyContent.set("Of course! It was so much fun. We should do it again!");

        case "Of course! It was so much fun. We should do it again!" -> 
            replyContent.set("Totally agree! Maybe next month?");

        case "Totally agree! Maybe next month?" -> 
            replyContent.set("Sounds good. Let's plan it soon.");

        case "What have you been up to lately?" -> 
            replyContent.set("Just busy with work and life. You?");

        case "Just busy with work and life. You?" -> 
            replyContent.set("Same here, but finally got some free time.");

        case "Same here, but finally got some free time." -> 
            replyContent.set("Great! We should definitely catch up.");

        case "Great! We should definitely catch up." -> 
            replyContent.set("Yes, maybe over coffee this weekend?");

        case "Yes, maybe over coffee this weekend?" -> 
            replyContent.set("Perfect, let's do it. Saturday afternoon?");

        case "Perfect, let's do it. Saturday afternoon?" -> 
            replyContent.set("Saturday works for me. See you then!");

        case "See you then!" -> 
            replyContent.set("Bye!!");

            default -> replyContent.set("Error");
        }

    }
}
