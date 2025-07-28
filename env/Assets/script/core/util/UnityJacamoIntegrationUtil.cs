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
using System.Text.RegularExpressions;
using WebSocketSharp;
using Debug = UnityEngine.Debug;


class UnityJacamoIntegrationUtil : MonoBehaviour
{

	private static string _jcmFilePath;
	private static string[] _fileLines;
	private static Process _jacamoProcess;

	//Utility used to configure .jcm file by adding agents
	public static void ConfigureJcmFile(GameObject[] avatars, GameObject[] envArtifacts)
	{
		var envManagerObject = envArtifacts.FirstOrDefault(envArtifact => envArtifact.GetComponent<EnvManager>() != null);

		if (envManagerObject != null)
			_jcmFilePath = envManagerObject.GetComponent<EnvManager>().jcmFilePath;
		else
			Debug.LogError("No GameObject with EnvManager component found in envArtifacts");

		var jcmContent = File.ReadAllText(_jcmFilePath);

		// get the mas name
		var masName = ExtractMasName(jcmContent);
		var workspaceContent = ConfigureEnvironmentArtifacts(envArtifacts, jcmContent, out var workspaceName);
		var agentsContent = GenerateAgentsContent(avatars, workspaceName);

		if (File.Exists(_jcmFilePath))
			File.Delete(_jcmFilePath);

		// Create the new JCM file content
		var newJcmContent = $"mas {masName} {{\n{agentsContent}\t{workspaceContent}}}\n";
		// Write the new content to the JCM file
		File.WriteAllText(_jcmFilePath, newJcmContent);

	}

	private static object GenerateAgentsContent(GameObject[] avatars, string workspaceName)
	{
		if (avatars == null || avatars.Length == 0)
			return "";

		var agentsContent = "";
		foreach (var avatar in avatars)
		{
			var avatarScript = avatar.GetComponent<AbstractAvatar>();
			agentsContent += BuildAgentDefinition(avatar, avatarScript, workspaceName);
		}

		return agentsContent;
	}

	private static string BuildAgentDefinition(GameObject avatar, AbstractAvatar avatarScript, string workspaceName)
	{
		const string agentClass = "vesna.VesnaAgent";
		const string localhost = "localhost";
		const string tabIndent = "\t\t";
		const string doubleTabIndent = "\t\t\t";
    
		var agentDef = $"\tagent {avatar.name}:{avatarScript.AgentFile} {{\n";
    
		// CLASS
		agentDef += $"{tabIndent}ag-class: {agentClass}\n";
    
		// BELIEFS
		agentDef += $"{tabIndent}beliefs: address( {localhost} )\n";
		agentDef += $"{doubleTabIndent}port( {avatarScript.port} )\n";
		agentDef += $"{doubleTabIndent}{avatarScript.AgentBeliefs.GetBeliefsAsLiterals()}\n";
    
		// GOALS
		if (avatarScript.Goals != null)
			agentDef += $"{tabIndent}goals:\t{string.Join(", ", avatarScript.Goals.Select(goal => goal.ToString().ToLower()))}\n";
    
		// WORKSPACE
		agentDef += $"{tabIndent}join: {workspaceName}\n";
    
		// FOCUS
		agentDef += BuildFocusSection(avatarScript.FocusedArtifacts, workspaceName, tabIndent, doubleTabIndent);
    
		agentDef += "\n\t}\n\n";
		return agentDef;
	}
	
	private static string BuildFocusSection(GameObject[] focusedArtifacts, string workspaceName, string tabIndent, string doubleTabIndent)
	{
		if (focusedArtifacts == null || focusedArtifacts.Length == 0)
			return "";
        
		var focusSection = $"{tabIndent}focus: ";
		foreach (GameObject artifact in focusedArtifacts)
		{
			focusSection += $"{workspaceName}.{artifact.name.FirstCharacterToLower()}\n";
			focusSection += doubleTabIndent;
		}
		return focusSection;
	}

	private static string ExtractMasName(string jcmContent)
	{
		var masName = Path.GetFileNameWithoutExtension(_jcmFilePath);
		var regex = new Regex(@"mas\s+(\w+)\s*\{([\s\S]*?)\}", RegexOptions.Multiline);
		var match = regex.Match(jcmContent);
		
		// If the mas name is found in the JCM content, use it; otherwise, use the file name
		if (match.Success)
			masName = match.Groups[1].Value;
		
		return masName;
	}

	private static string ConfigureEnvironmentArtifacts(GameObject[] envArtifacts, string jcmContent, out string workspaceName)
	{
		var regex = new Regex(@"workspace\s+(\w+)\s*\{([\s\S]*?)\}", RegexOptions.Multiline);
		var match = regex.Match(jcmContent);

		workspaceName = "ws"; // default workspace name
		if (match.Success)
			workspaceName = match.Groups[1].Value;
		var updatedContent = "";
		
		foreach (var envArtifact in envArtifacts)
		{
			var script = envArtifact.GetComponent<Artifact>();
			
			print("Analyze " + envArtifact.name);
			print(" of type: " + script.ArtifactType);
			
			var artifact = "\t\t" + $@"artifact {envArtifact.name.FirstCharacterToLower()}: artifact.{script.ArtifactType}Artifact(" + "\"" + envArtifact.name + "\", " + script.Port;
			
			if (string.IsNullOrEmpty(script.Port)) // If the port is not set, there are no arguments
				artifact = "\t\t" + $@"artifact {envArtifact.name.FirstCharacterToLower()}: artifact.{script.ArtifactType}Artifact(";
			
			// If the artifact has properties, add them
			if (!script.ArtifactProperties.IsNullOrEmpty())
			{
				artifact += $@", ""{script.ArtifactProperties}"")";
			}
			else
				artifact += ")";
			
			artifact += "\n";
			updatedContent += artifact;
		}
		return $"workspace {workspaceName} {{\n" + updatedContent + "\t}\n";
	}

	// Open JaCaMo application
	//TODO: Fix this by finding the root folder (before "env") and then going to the "mind" folder
	public static async Task RunJaCaMoApp()
	{
		var isWindows = RuntimeInformation.IsOSPlatform( OSPlatform.Windows );
		var mindPath = GetJacamoPath(Application.dataPath);
		var gradleWrapper = isWindows ? "gradlew.bat" : "./gradlew";
		var command = $"{gradleWrapper} run";
		var psi = new ProcessStartInfo {
			FileName = isWindows ? "cmd.exe" : "/bin/bash",
			Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = mindPath
		};

		try
		{
			_jacamoProcess = Process.Start(psi);
			var output = await _jacamoProcess.StandardOutput.ReadToEndAsync();
			var error  = await _jacamoProcess.StandardError.ReadToEndAsync();
			
			_jacamoProcess.WaitForExit();

			print( "[JACAMO] Gradle output:\n" + output );
			if ( ! string.IsNullOrEmpty( error ) )
				print( "[JACAMO] Gradle error:\n" + error );
		} catch ( Exception e ) {
			print( "[JACAMO] Error during gradle start: " + e.Message );
		}
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
	private static string GetJacamoPath(string startingPath)
	{
		var dir = new DirectoryInfo(startingPath);
		while (dir != null)
		{
			var mindPath = Path.Combine(dir.FullName, "mind");
			if (Directory.Exists(mindPath))
				return mindPath;
			dir = dir.Parent;
		}
		
		throw new DirectoryNotFoundException("Could not find the mind folder");
	}
}
