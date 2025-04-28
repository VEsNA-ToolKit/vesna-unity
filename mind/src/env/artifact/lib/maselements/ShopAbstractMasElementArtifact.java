package artifact.lib.maselements;

import artifact.lib.model.item.ShopItemEnum;
import artifact.lib.model.item.ItemInfoModel;
import artifact.lib.utils.AgentUtils;
import cartago.*;
import com.fasterxml.jackson.core.type.TypeReference;
import jason.asSyntax.*;
import lombok.Data;

import java.util.LinkedList;
import java.util.List;
import java.util.Queue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;

// @Data
public abstract class ShopAbstractMasElementArtifact extends AbstractMasElementArtifact {

    protected List<ItemInfoModel> itemList;
    private final String inventory = "inventory";
    private Queue<String> agentQueue;
    private String currentAgentToServe = "";
    protected String propertyName = "";

    @OPERATION
    protected void init(String artifactName, int port, String propertyName){
        super.init(artifactName, port);
        // Initialize the queue
        agentQueue = new LinkedList<>();
        this.propertyName = propertyName;
    }

    @OPERATION
    public void joinQueue(String agentName){
        try {
            writeLog("Attempting to lock for agent: " + agentName);
            lock.lock();
            writeLog("Lock acquired for agent: " + agentName);
            if (!agentQueue.contains(agentName)) {
                agentQueue.add(agentName);
                writeLog("Agent " + agentName + " joined the queue. Queue size: " + agentQueue.size());
                // If this is the first agent and no one is being served, make them current
                if (agentQueue.size() == 1 && currentAgentToServe.equals("")) {
                    writeLog("Agent " + agentName + " is the first client. Set as currentAgent");
                    currentAgentToServe = agentName;
                    // Signal agent your_turn(true)
                    try {
                        // Signal agent your_turn(true)
                        execInternalOp("signalAgent", agentName, "your_turn", true);
                        writeLog("Signal sent successfully to next agent to be served " + agentName);
                    } catch (Exception e) {
                        writeLog("Failed to signal agent " + agentName + ": " + e.getMessage());
                    }
                }
            }
        } finally {
            lock.unlock();
            writeLog("Lock released for agent: " + agentName);
        }
    }

    @OPERATION
    public void dequeue(String agentName){
        try {
            lock.lock();
            agentQueue.remove(agentName);
            writeLog("Agent " + agentName + " leaving the queue. Queue size: " + agentQueue.size());
            if (agentQueue.peek() == null) {
                setAgentToServe("");
                writeLog("Finished to serve all agents for now");
            } else { // Serve next agent
                setAgentToServe(agentQueue.peek());
                writeLog("Next agent to serve: " + agentQueue.peek());
                // Signal the next agent your_turn(true)
                execInternalOp("signalAgent", agentQueue.peek(), "your_turn", true);
                writeLog("Signal sent successfully to next agent to be served " + agentQueue.peek());
            }
        } finally {
            lock.unlock();
        }
    }

    @INTERNAL_OPERATION
    protected void setAgentToServe(String newAgent) {
        // Update the property value
        currentAgentToServe = newAgent;
    }

    @OPERATION
    protected void getItemInfo(String agentRequest, OpFeedbackParam<Literal> itemInfoResponse) {
        try {
            lock.lock();
            Literal literal = Literal.parseLiteral(agentRequest);
            String agentName = literal.getTerm(0).toString();
            String itemName = literal.getTerm(1).toString();
            writeLog("Agent " + agentName + " asked for: " + itemName);
            // Filter for the given item name
            ItemInfoModel itemInfoModel = retrieveItemByName(itemName);
            // Evaluate if item is available
            Literal literalResponse;
            if (itemInfoModel == null) {
                writeLog("Item " + itemName + " is not selled in this shop");
                literalResponse = AgentUtils.convertObjectToLiteral(new ItemInfoModel(ShopItemEnum.NoValue, 0, 0));
            } else if (itemInfoModel.getQuantity() > 0) {
                writeLog("Item " + itemName + " available. " + itemInfoModel);
                literalResponse = AgentUtils.convertObjectToLiteral(itemInfoModel);
            } else { // Item has finished. Shop need refill
                writeLog("Item " + itemName + " has finished.");
                literalResponse = AgentUtils.convertObjectToLiteral(itemInfoModel);
            }
            itemInfoResponse.set(literalResponse);
        } catch (Exception e) {
            logger.warning(e.getMessage());
        } finally {
            lock.unlock();
        }
    }

    @OPERATION
    protected void buyItem(String agentRequest) {
        Condition condition = lock.newCondition();
        try {
            lock.lock();
            Literal literal = Literal.parseLiteral(agentRequest);
            String agentName = literal.getTerm(0).toString();
            String itemName = literal.getTerm(1).toString();
            int itemQuantity = Integer.parseInt(literal.getTerm(2).toString());
            writeLog("Agent " + agentName + " is going to buy " + itemName + " of quantity: " + itemQuantity);
            // Retrieve item list
            itemList = getProperty(propertyName, new TypeReference<>() {});
            // Simulate another 3-second wait
            condition.await(2, TimeUnit.SECONDS);
            itemList.forEach(item -> {
                if(itemName.equals(item.getItemName().toString())){
                    item.setQuantity(item.getQuantity() - itemQuantity);
                    writeLog("Agent " + agentName + " has bought " + itemQuantity + " of " + itemName + ". Remaining items: "
                            + item.getQuantity());
                    // Refill if needed
                    if(item.getQuantity() == 0) {
                        writeLog("Calling inventory to refill " + itemName);
                        execInternalOp("signalAllAgents", "refill", List.of(artifactName, itemName).toArray());
                    }
                }
            });
            // Update property
            setProperty(propertyName, itemList);
        } catch (Exception e) {
            logger.warning(e.getMessage());
        } finally {
            lock.unlock();
        }
    }

    @OPERATION
    protected void refill(String request){
        try{
            lock.lock();
            Literal literal = Literal.parseLiteral(request);
            String agentName = literal.getTerm(0).toString();
            StringTermImpl stringTerm = (StringTermImpl) literal.getTerm(1);
            String itemName = stringTerm.getString();
            writeLog("Operator " + agentName + " arrived. Refill " + itemName);
            itemList = getProperty(propertyName, new TypeReference<>() {});
            itemList.forEach(item -> {
                if(itemName.equals(item.getItemName().toString())){
                    item.setQuantity(100);
                    writeLog("Agent " + agentName + " refilled the shop ");
                }
            });
            // Update property
            setProperty(propertyName, itemList);
            itemList = getProperty(propertyName, new TypeReference<>() {});
            writeLog("Artifact properties: " + itemList.toString());
        }finally {
            lock.unlock();
        }
    }

    protected ItemInfoModel retrieveItemByName(String itemName){
        // Retrieve property
        itemList = getProperty(propertyName, new TypeReference<>() {
        });

        return itemList.stream()
                .filter(item -> itemName.equals(item.getItemName().getDisplayName()))
                .findFirst()
                .orElse(null);
    }
}
