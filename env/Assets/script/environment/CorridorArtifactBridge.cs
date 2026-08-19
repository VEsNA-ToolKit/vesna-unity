using UnityEngine;

// Da attaccare al GameObject padre del Corridoio, insieme a CorridorData
public class CorridorArtifactBridge : AbstractMasElement
{
    public string artifactName => corridorData != null ? corridorData.corridorId : gameObject.name;
    private CorridorData corridorData;

    void Awake()
    {
        if (!Application.IsPlaying(gameObject)) return;
        
         objInUse = gameObject;

        corridorData = GetComponent<CorridorData>();
        
        
        // Inizializza WebSocket
        port = PortAssigner.GetPort(artifactName);
        initializeWebSocketConnection(OnMessageFromJacamo);
        _ = startServer();
    }

    void Start()
    {
        // Appena parte la scena, manda a JaCaMo le posizioni fisiche dei poli
        SendPolesToJacamo();
    }

    private void SendPolesToJacamo()
    {
        if (corridorData == null) return;

        var payload = new {
            p1_x = corridorData.Pole1Position.x,
            p1_z = corridorData.Pole1Position.z,
            p2_x = corridorData.Pole2Position.x,
            p2_z = corridorData.Pole2Position.z
        };

        wsChannel?.sendMessage(
            UnityJacamoIntegrationUtil.CreateAndConvertJacamoMessageIntoJsonString(
                "init_poles", null, null, artifactName, payload
            )
        );
        
        Debug.Log($"[CorridorBridge] Inviate posizioni poli per {artifactName}");
    }

    private void OnMessageFromJacamo(object sender, WebSocketSharp.MessageEventArgs e)
    {
        // Il corridoio è statico, JaCaMo non dovrebbe mandare comandi fisici al corridoio,
        // ma se in futuro servirà, lo gestirai qui.
    }
}