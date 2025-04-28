package artifact.lib.model;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class WsMessage {

    private String messageType;
    private String messagePayload;
    // Event to signal to the agent
    private String agentEvent;
    private String agentName;
    // params that could be boolean, List<String>, etc...
    private Object param;

}
