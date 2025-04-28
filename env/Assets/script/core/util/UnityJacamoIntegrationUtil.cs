using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using System.Runtime.InteropServices;


class UnityJacamoIntegrationUtil : MonoBehaviour
{

	private static string jcmFilePath = "../mind/supermarket.jcm";
	private static string[] fileLines = {
		"mas supermarket {\n",
		"\tworkspace w {\n",
	"}"
	};

	//Utility used to configure .jcm file by adding agents
	public static void ConfigureJcmFile(GameObject[] avatars, GameObject[] envArtifacts)
	{
		if (File.Exists(jcmFilePath))
		{
			File.Delete(jcmFilePath);
		}

		// Append HEADERS
		File.AppendAllText(jcmFilePath, fileLines[0]);

		// Configure artifacts
		foreach (GameObject envArtifact in envArtifacts)
		{
			Artifact script = envArtifact.GetComponent<Artifact>();
			print("Analize " + envArtifact.name);
			print(" of type: " + script.ArtifactType);
			string artifact = "\t\t" + $@"artifact {envArtifact.name.FirstCharacterToLower()}: artifact.{script.ArtifactType.ToString()}Artifact({"\"" + envArtifact.name + "\""}, {script.Port}";
			if (script.ArtifactProperties != null)
			{
				artifact += $@", ""{script.ArtifactProperties}"")";
			}
			else
			{
				artifact += ")";
			}
			artifact += "\n";
			fileLines[1] += artifact;
		}

		fileLines[1] += "\t}\n";

		// Configure all agents
		if (avatars != null && avatars.Length != 0)
		{
			addAgents(avatars);
		}

		// FOOT
		File.AppendAllText(jcmFilePath, fileLines[1] + Environment.NewLine);
		File.AppendAllText(jcmFilePath, fileLines[2]);

	}

	private static void addAgents(GameObject[] avatars)
	{
		foreach (GameObject avatar in avatars)
		{
			AbstractAvatar avatarScript = avatar.GetComponent<AbstractAvatar>();
			string ag_def = $"\tagent {avatar.name}:{avatarScript.AgentFile} {{\n";
			// CLASS
			ag_def += "\t\tag-class: vesna.VesnaAgent\n";
			// BELIEFS
			ag_def += $"\t\tbeliefs: address( localhost )\n";
			ag_def += $"\t\t\tport( {avatarScript.port} )\n";
			ag_def += $"\t\t\t{avatarScript.AgentBeliefs.GetBeliefsAsLiterals()}\n";
			// GOALS
			if ( avatarScript.Goals != null )
				ag_def += $"\t\tgoals:\t{string.Join( ", ", avatarScript.Goals.Select( goal => goal.ToString().ToLower() ) ) }\n";
			// WORKSPACE
			ag_def += "\t\tjoin: w\n";
			// FOCUS
			if ( avatarScript.FocusedArtifacts != null && avatarScript.FocusedArtifacts.Length != 0 ) {
				ag_def += "\t\tfocus: ";
				foreach( GameObject art in avatarScript.FocusedArtifacts ) {
					ag_def += $"w.{art.name.FirstCharacterToLower()}\n";
					ag_def += "\t\t\t";
				}
			}
			ag_def += "\n\t}\n\n";

			File.AppendAllText(jcmFilePath, ag_def);
		}
	}

	// Open JaCaMo application
	public static async Task RunJaCaMoApp()
	{
		string command = "gradle run";
		bool isWindows = RuntimeInformation.IsOSPlatform( OSPlatform.Windows );
		var psi = new ProcessStartInfo {
			FileName = isWindows ? "cmd.exe" : "/bin/bash",
			Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = System.IO.Path.Combine( Application.dataPath, "../mind" )
		};

		try {
			using ( var process = Process.Start( psi ) ) {
				string output = process.StandardOutput.ReadToEnd();
				string error = process.StandardError.ReadToEnd();
				process.WaitForExit();

				print( "[JACAMO] Gradle output:\n" + output );
				if ( ! string.IsNullOrEmpty( error ) )
					print( "[JACAMO] Gradle error:\n" + error );
			}
		} catch ( Exception e ) {
			print( "[JACAMO] Error during gradle start: " + e.Message );
		}
		// Process jacamoProcess;
		// string jacamoFolderPath = Path.Combine( Directory.GetCurrentDirectory(), "../mind/" );
		// string gradleCommand = "gradle run";

		// await Task.Run(() =>
		// {
		//     jacamoProcess = new Process();
		//     jacamoProcess.StartInfo.WorkingDirectory = jacamoFolderPath;
		//     jacamoProcess.StartInfo.FileName = "/bin/bash"; // For Windows
		//     jacamoProcess.StartInfo.Arguments = $"/c \"cd '{jacamoFolderPath}' && {gradleCommand}\"";
		//     jacamoProcess.StartInfo.UseShellExecute = false;
		//     jacamoProcess.StartInfo.RedirectStandardOutput = true; // Capture output
		//     jacamoProcess.StartInfo.RedirectStandardError = true;
		//     jacamoProcess.StartInfo.CreateNoWindow = true; // Hide the command window

		//     try
		//     {
		//         // Start the process
		//         jacamoProcess.Start();
		//         string output = "";
		//         // Read the output to determine if Jacamo has successfully started
		//         while (!jacamoProcess.HasExited) // Keep reading while the process is running
		//         {
		//             output = jacamoProcess.StandardOutput.ReadLine();
		//             if (!string.IsNullOrEmpty(output))
		//             {
		//                 // Check for the signal from the .bat file
		//                 if (output.Contains("JACAMO_LAUNCH_SUCCESSFUL"))
		//                 {
		//                     print("Jacamo Output: " + output);
		//                     break;
		//                 }
		//             }
		//         }
		//     }
		//     catch (Exception ex)
		//     {
		//         print("Error starting Jacamo application: " + ex.Message);
		//     }

		// });
	}

	// Starts web socket connections for avatars and environment objects
	public static async Task StartWebSocketConnections(GameObject[] avatars, GameObject[] environmentArtifacts)
	{
		List<Task> tasks = new List<Task>();

		print("Connecting avatars...");
		// Start avatar web socket connections
		foreach (GameObject avatar in avatars)
		{
			AbstractAvatar avatarScript = avatar.GetComponent<AbstractAvatar>();

			if (avatarScript == null)
			{
				throw new Exception("The avatar" + avatar.name + " has not script Avatar");
			}

			// tasks.Add(avatarScript.connectWs());
			tasks.Add( avatarScript.startServer() );
		}

		await Task.WhenAll(tasks);

		print("Connecting envArtifacts...");
		// Start web socket connections
		foreach (GameObject envArtifact in environmentArtifacts)
		{
			Artifact artifactScript = envArtifact.GetComponent<Artifact>();
			if (artifactScript == null)
			{
				throw new Exception("The artifact" + envArtifact.name + " has not script.");
			}

			// tasks.Add(artifactScript.connectWs());
			tasks.Add( artifactScript.startServer() );
		}

		await Task.WhenAll(tasks);
	}

	public static string createAndConvertJacamoMessageIntoJsonString(string messageType,
		string messagePayload, string agentEvent, string agentName, object param)
	{
		// In sight: messagePayload is null. agentName is null. param può essere o la stringa del nome o una struttura
	 // artifactInfo.
		// // WsMessage wsMessage = new WsMessage(messageType, messagePayload, agentEvent, agentName, param);
		// // return JsonConvert.SerializeObject(wsMessage);
		BrainMessage wsMsg;
		switch ( messageType ){
			case "eyes":
				SightData sightData;
				if ( agentEvent == "artifactSeen" ) {
					ArtifactInfo art_info = ( ArtifactInfo ) param;
					sightData = new SightData( "artifact", art_info.ArtifactType, art_info.ArtifactName );
				} else if ( agentEvent == "agentSeen" ) {
					sightData = new SightData( "agent", null, param.ToString() );
				} else {
					sightData = new SightData( "null", null, "error" );
				}
				wsMsg = new BrainMessage( "body", "vesna", "sight", sightData );
				break;

			case "destinationReached":
				MoveData moveData;
				if ( agentEvent == "reached_friend" ) {
					moveData = new MoveData( "friend", param.ToString() );
				} else if ( agentEvent == "reached_destination" ) {
					moveData = new MoveData( "place", param.ToString() );
				} else {
					moveData = new MoveData( "error", "error" );
				}
				wsMsg = new BrainMessage( "body", "vesna", "movement", moveData );
				break;
				
// 			case "counter":


			case "supermarketDoorStatus":
				DoorData doorData = new DoorData( (bool) param );
				wsMsg = new BrainMessage( "body", "vesna", "door", doorData );
				break;

			case "artifactStrategy":
				ArtsData artsData = new ArtsData( (string[]) param );
				wsMsg = new BrainMessage( "body", agentName, "arts_info", artsData );
				break;

			default:
				wsMsg = new BrainMessage( "body", "vesna", "error", null );
				break;
		}
		return JsonConvert.SerializeObject( wsMsg );
	}
}
