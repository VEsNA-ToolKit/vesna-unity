using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace script.core.util
{
    
    public class ArtifactProperty
    {
        public string Name;
        public string Type;
        public object Value;
    }
    
    public static class ArtifactResolver
    {
        private static readonly Dictionary<string, System.Type> InfoTypeMap = new()
        {
            { "coffee", typeof(CoffeeInfo) },
            { "clothes", typeof(ClothesInfo) },
            { "fruit", typeof(FruitInfo) }
        };
        
        private static readonly string EditorPrefsKey = "ArtifactSourcePath";
        private static string ArtifactSourcePath => EditorPrefs.GetString(EditorPrefsKey, "");
        private static readonly Dictionary<string, string> ParentMap = new();
        private static readonly Dictionary<string, List<ArtifactProperty>> PropertyMap = new();
        
        // ---------------------- MENU ITEMS ----------------------
        
        [MenuItem("Artifacts/Set Artifact Source Path")]
        public static void SetArtifactSourcePath()
        {
            var path = EditorUtility.OpenFolderPanel("Select Artifact Source Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString(EditorPrefsKey, path);
                DisplayOkDialog("Path Set", $"Artifact source path set to: {path}");
            }
            else
            {
                DisplayOkDialog("Path Not Set", "No path was selected.");
            }
        }
        
        [MenuItem("Artifacts/Scan Artifacts Folder")]
        public static void ScanArtifactsFolder()
        {
            if (!Directory.Exists(ArtifactSourcePath))
            {
                UnityEngine.Debug.LogError($"Artifact source path does not exist: {ArtifactSourcePath}");
                return;
            }
        
            ScanArtifactsInternal();
            
            var count = PropertyMap.Count;
            if (count == 0)
            {
                DisplayOkDialog("No Artifacts Found", $"No .java files found in {ArtifactSourcePath}");
                return;
            }
            
            DisplayOkDialog("Scan Complete", $"Scanned {count} artifact files in {ArtifactSourcePath}");
        }
        
        // Validation method to enable/disable the menu item
        [MenuItem("Artifacts/Scan Artifacts Folder", true)]
        public static bool ValidateScanArtifacts()
        {
            // return true = enabled, false = greyed out
            return !string.IsNullOrEmpty(ArtifactSourcePath);
        }
        
        // ---------------------- PARSER ----------------------
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Auto-scan when Unity reloads or enters Play mode
            if (!string.IsNullOrEmpty(ArtifactSourcePath) && Directory.Exists(ArtifactSourcePath))
            {
                ScanArtifactsInternal();
            }
        }
        
        private static void ParseArtifact(string filePath)
        {
            var content = File.ReadAllText(filePath);

            // Match "class ClassName extends ParentName"
            var match = System.Text.RegularExpressions.Regex.Match(
                content,
                @"class\s+(\w+)(?:\s+extends\s+(\w+))?",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            if (!match.Success)
            {
                Debug.LogWarning("Could not parse class declaration in file: " + filePath);
                return;
            }
            
            var className = match.Groups[1].Value;
            var parentName = match.Groups[2].Success ? match.Groups[2].Value : null;
            
            // find all properties defined with defineObsProperty or initializeProperty
            var propertyMatches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"(defineObsProperty|initializeProperty)\s*\(\s*""([^""]+)""\s*,\s*([^)]*)\)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            
            var properties = new List<ArtifactProperty>();
            foreach (System.Text.RegularExpressions.Match propMatch in propertyMatches)
            {
                var propName = propMatch.Groups[2].Value;
                var valueExpr = propMatch.Groups[3].Value;
                var inferredType = InferTypeFromValue(valueExpr);

                properties.Add(new ArtifactProperty {
                    Name = propName,
                    Type = inferredType,
                    Value = ConvertValueToType(valueExpr, inferredType)
                });
            }
            
            ParentMap[className] = parentName;
            PropertyMap[className] = properties;
        }
        
        public static List<ArtifactProperty> GetAllProperties(string className)
        {
            var result = new List<ArtifactProperty>();
            var visited = new HashSet<string>();

            // Traverse the inheritance chain
            while (!string.IsNullOrEmpty(className) && !visited.Contains(className))
            {
                visited.Add(className);

                // add properties of the current class
                if (PropertyMap.TryGetValue(className, out var props))
                {
                    // Add properties if not already in the result
                    foreach (var prop in props.Where(p => !result.Contains(p)))
                    {
                        result.Add(prop);
                    }
                }

                // Go to the parent class
                if (!ParentMap.TryGetValue(className, out className))
                    break;
            }
            
            // Reverse the list to have base class properties first
            result.Reverse();

            return result;
        }
        
        // HELPER METHODS
        private static void DisplayOkDialog(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }
        private static object ConvertValueToType(string valueExpr, string typeName)
        {
            valueExpr = valueExpr.Trim();
    
            switch (typeName)
            {
                case "string":
                    var trimmed = valueExpr.Trim('"');
                    return trimmed == "null" ? "" : trimmed;
                case "bool":
                    return bool.Parse(valueExpr);
                case "float":
                    return float.Parse(valueExpr, System.Globalization.CultureInfo.InvariantCulture);
                case "list":
                    return InstantiateListByType(valueExpr);
                default:
                    return valueExpr; // Return as string if type is unknown
            }
        }

        private static object InstantiateListByType(string valueExpr)
        {
            // Check property name for matching keyword
            var lowerPropName = valueExpr.ToLower();
                    
            // Try to match known types, otherwise return List<object>
            foreach (var listType in from kvp in InfoTypeMap where lowerPropName.Contains(kvp.Key) select typeof(List<>).MakeGenericType(kvp.Value))
            {
                return System.Activator.CreateInstance(listType); // Create an empty list of the matched type
            }
                    
            throw new System.Exception($"Could not infer list type for property: {valueExpr}, please reference the documentation.");
        }

        private static string InferTypeFromValue(string valueExpr)
        {
            valueExpr = valueExpr.Trim();

            if (IsStringLiteral(valueExpr))
                return "string";

            if (IsBooleanLiteral(valueExpr))
                return "bool";

            if (IsFloatLiteral(valueExpr))
                return "float";

            return IsListExpression(valueExpr) ? "list" :
                // default fallback
                "unknown";
        }
        
        // Helper methods
        private static bool IsStringLiteral(string valueExpr)
        {
            return valueExpr.StartsWith("\"") && valueExpr.EndsWith("\"");
        }

        private static bool IsBooleanLiteral(string valueExpr)
        {
            return valueExpr.Equals("true") || valueExpr.Equals("false");
        }

        private static bool IsFloatLiteral(string valueExpr)
        {
            return double.TryParse(valueExpr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        private static bool IsListExpression(string valueExpr)
        {
            return valueExpr.Contains("List") || valueExpr.StartsWith("[") || valueExpr.EndsWith("]");
        }
        
        private static void ScanArtifactsInternal()
        {
            var artifactFiles = Directory.GetFiles(ArtifactSourcePath, "*.java", SearchOption.TopDirectoryOnly);

            foreach (var file in artifactFiles)
            {
                ParseArtifact(file);
            }
        }

    }
}