package artifact.lib.maselements;

import artifact.lib.maselements.AbstractMasElementAgent;
import cartago.OPERATION;
import cartago.OpFeedbackParam;

public class AgentMasElement extends AbstractMasElementAgent {

    @Override
    @OPERATION
    public void processConversation(String content, OpFeedbackParam<String> replyContent) {

        switch (content) {
            case "Hi! How are you?" -> //Message sent from the agent who reached his friend
                    replyContent.set("How long! I'm fine and you?");
            case "How long! I'm fine and you?" -> replyContent.set("I'm fine thanks. See you next time!");
            case "I'm fine thanks. See you next time!", "Bye!!" -> replyContent.set("Bye!!");
        }

    }
}
