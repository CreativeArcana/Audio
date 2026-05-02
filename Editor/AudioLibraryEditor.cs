using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreativeArcana.Audio.Editor
{
    [CustomEditor(typeof(AudioLibrary))]
    public sealed class AudioLibraryEditor : UnityEditor.Editor
    {
        private const string PrefsPrefix = "CreativeArcana.Audio.AudioLibrary";

        private SerializedProperty _entriesProperty;

        private bool _autoGenerateCode;
        private string _generatedCodeOutputFolder;

        private string _autoGeneratePrefsKey;
        private string _outputFolderPrefsKey;

        private void OnEnable()
        {
            _entriesProperty = serializedObject.FindProperty("_entries");

            var library = (AudioLibrary)target;
            var guid = GetLibraryPersistentKey(library);

            _autoGeneratePrefsKey = $"{PrefsPrefix}.{guid}.AutoGenerateCode";
            _outputFolderPrefsKey = $"{PrefsPrefix}.{guid}.GeneratedCodeOutputFolder";

            _autoGenerateCode = EditorPrefs.GetBool(_autoGeneratePrefsKey, false);
            _generatedCodeOutputFolder = EditorPrefs.GetString(_outputFolderPrefsKey, string.Empty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_entriesProperty, true);
            var entriesChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            DrawCodeGenerationSection();

            if (entriesChanged)
            {
                var library = (AudioLibrary)target;
                EditorUtility.SetDirty(library);

                if (_autoGenerateCode)
                    Generate(library);
            }
        }

        private void DrawCodeGenerationSection()
        {
            var library = (AudioLibrary)target;
            var outputFolder = ResolveOutputFolder(library);
            var isValidOutputFolder = IsAssetsPath(outputFolder);

            var enumName = GetGeneratedEnumName(library);
            var mapperName = $"{enumName}Extensions";
            var enumPath = $"{outputFolder}/{enumName}.g.cs";
            var mapperPath = $"{outputFolder}/{mapperName}.g.cs";

            EditorGUILayout.LabelField("Code Generation", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _autoGenerateCode = EditorGUILayout.ToggleLeft("Auto Generate On Library Changes", _autoGenerateCode);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Output Folder");
                EditorGUILayout.SelectableLabel(GetDisplayOutputFolder(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button("Browse...", GUILayout.Width(90)))
                    BrowseOutputFolder();

                if (GUILayout.Button("Default", GUILayout.Width(70)))
                    _generatedCodeOutputFolder = string.Empty;
            }

            if (EditorGUI.EndChangeCheck())
                SaveEditorPrefs();

            EditorGUILayout.Space(4);

            if (!isValidOutputFolder)
            {
                EditorGUILayout.HelpBox("Output folder must be inside the project's Assets folder.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"Generated Files:\n{enumPath}\n{mapperPath}", MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = isValidOutputFolder;
                if (GUILayout.Button("Generate"))
                    Generate(library);

                if (GUILayout.Button("Reveal"))
                    RevealOutputFolder(outputFolder);

                GUI.enabled = true;
            }
        }

        private void BrowseOutputFolder()
        {
            var defaultFolder = GetAbsoluteDefaultFolder();

            var selectedAbsolutePath = EditorUtility.OpenFolderPanel(
                "Select AudioIds Output Folder",
                defaultFolder,
                string.Empty);

            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
                return;

            selectedAbsolutePath = selectedAbsolutePath.Replace("\\", "/");

            if (!TryConvertAbsolutePathToAssetsPath(selectedAbsolutePath, out var assetsPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Please select a folder inside the project's Assets folder.",
                    "OK");
                return;
            }

            _generatedCodeOutputFolder = assetsPath;
            SaveEditorPrefs();
        }

        private void Generate(AudioLibrary library)
        {
            if (library == null)
                return;

            var outputFolder = ResolveOutputFolder(library);
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                EditorUtility.DisplayDialog("Generate Failed", "Could not resolve output folder.", "OK");
                return;
            }

            AudioIdCodeGenerator.RequestGenerate(library, outputFolder);
        }

        private string ResolveOutputFolder(AudioLibrary library)
        {
            if (!string.IsNullOrWhiteSpace(_generatedCodeOutputFolder))
                return NormalizeAssetsPath(_generatedCodeOutputFolder);

            var libraryAssetPath = AssetDatabase.GetAssetPath(library);
            if (string.IsNullOrWhiteSpace(libraryAssetPath))
                return "Assets";

            var directory = Path.GetDirectoryName(libraryAssetPath);
            return string.IsNullOrWhiteSpace(directory) ? "Assets" : NormalizeAssetsPath(directory);
        }

        private string GetDisplayOutputFolder()
        {
            return string.IsNullOrWhiteSpace(_generatedCodeOutputFolder)
                ? "<Same folder as AudioLibrary>"
                : NormalizeAssetsPath(_generatedCodeOutputFolder);
        }

        private string GetAbsoluteDefaultFolder()
        {
            var library = (AudioLibrary)target;
            var resolvedFolder = ResolveOutputFolder(library);

            if (TryConvertAssetsPathToAbsolutePath(resolvedFolder, out var absolutePath))
                return absolutePath;

            return Application.dataPath;
        }

        private void RevealOutputFolder(string assetsPath)
        {
            if (!TryConvertAssetsPathToAbsolutePath(assetsPath, out var absolutePath))
                return;

            if (!Directory.Exists(absolutePath))
                Directory.CreateDirectory(absolutePath);

            EditorUtility.RevealInFinder(absolutePath);
        }

        private void SaveEditorPrefs()
        {
            EditorPrefs.SetBool(_autoGeneratePrefsKey, _autoGenerateCode);
            EditorPrefs.SetString(_outputFolderPrefsKey, _generatedCodeOutputFolder ?? string.Empty);
        }

        private static bool TryConvertAbsolutePathToAssetsPath(string absolutePath, out string assetsPath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            var assetsAbsolutePath = Application.dataPath.Replace("\\", "/");

            if (!absolutePath.StartsWith(assetsAbsolutePath))
            {
                assetsPath = null;
                return false;
            }

            assetsPath = "Assets" + absolutePath.Substring(assetsAbsolutePath.Length);
            assetsPath = NormalizeAssetsPath(assetsPath);
            return true;
        }

        private static bool TryConvertAssetsPathToAbsolutePath(string assetsPath, out string absolutePath)
        {
            assetsPath = NormalizeAssetsPath(assetsPath);

            if (!IsAssetsPath(assetsPath))
            {
                absolutePath = null;
                return false;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                absolutePath = null;
                return false;
            }

            absolutePath = Path.Combine(projectRoot, assetsPath).Replace("\\", "/");
            return true;
        }

        private static bool IsAssetsPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   NormalizeAssetsPath(path).StartsWith("Assets");
        }

        private static string NormalizeAssetsPath(string path)
        {
            return path.Replace("\\", "/");
        }

        private static string GetLibraryPersistentKey(AudioLibrary library)
        {
            var assetPath = AssetDatabase.GetAssetPath(library);
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrWhiteSpace(guid))
                    return guid;
            }

            return library.name;
        }

        private static string GetGeneratedEnumName(AudioLibrary library)
        {
            var libraryName = library != null && !string.IsNullOrWhiteSpace(library.name)
                ? library.name
                : "AudioLibrary";

            return SanitizeIdentifier(libraryName) + "Ids";
        }

        private static string SanitizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Audio";

            var result = value.Replace(" ", "_").Replace("-", "_");

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }
    }
}
