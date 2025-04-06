using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRC.Core;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;

public class RenderDocLauncher : EditorWindow
{
    private string _qrenderdocExePath;
    private string _vrcExePath;
    private string _randomRoomId;
    private string _lastBuildUrl;
    private string _vrcArgs;
    private void OnGUI()
    {
        EditorGUILayout.LabelField("RenderDoc Settings", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 200;
        _qrenderdocExePath = EditorGUILayout.TextField("RenderDocCmd Path", _qrenderdocExePath);
        _vrcExePath = EditorGUILayout.TextField("VRChat.exe Path", _vrcExePath);
        _randomRoomId = EditorGUILayout.TextField("Random Room Id", _randomRoomId);
        _lastBuildUrl = EditorGUILayout.TextField("Last Build URL", _lastBuildUrl);
        _vrcArgs = EditorGUILayout.TextField("VRChat Args", _vrcArgs);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Inputs"))
        {
            Reload();
        }
        if (GUILayout.Button("Test last build with RenderDoc"))
        {
            TestLastBuildWithRenderDoc();
        }
        if (GUILayout.Button("Build & Test with RenderDoc"))
        {
            BuildAndTestWithRenderDoc();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("⚠ if you build and test with RenderDoc, you must open VRC SDK builder tab first ⚠", EditorStyles.largeLabel);
    }

    private void Reload()
    {
        _qrenderdocExePath = $"C:\\Program Files\\RenderDoc\\qrenderdoc.exe";
        _vrcExePath = GetVRCExePath();
        _randomRoomId = VRC.Tools.GetRandomDigits(10);
        _lastBuildUrl = UnityWebRequest.EscapeURL(EditorPrefs.GetString("lastVRCPath")).Replace("+", "%20");
        _vrcArgs = "--enable-debug-gui --enable-sdk-log-levels --enable-udon-debug-logging --watch-worlds";
    }
    private async void TestLastBuildWithRenderDoc()
    {
        string temppath = Path.Combine(Application.dataPath, "../", FileUtil.GetUniqueTempPathInProject());
        Directory.CreateDirectory(temppath);
        TextAsset cap = Resources.Load<TextAsset>("renderdoc");
        string replaceArgs = $"--url=create?roomId={_randomRoomId}&hidden=true&name=BuildAndRun&url=file:///{_lastBuildUrl} {_vrcArgs}";
        string replaceExe = $"{_vrcExePath}".Replace(@"\", @"\\");
        string rawcap = cap.text;
        rawcap = rawcap.Replace("REPLACE_ARGS", replaceArgs);
        rawcap = rawcap.Replace("REPLACE_EXE", replaceExe);
        string capPath = Path.Combine(temppath, "renderdoc.cap");
        File.WriteAllText(capPath, rawcap);
        string cmd = $"\"{_qrenderdocExePath}\" \"{capPath}\"";
        Debug.Log($"<color=green>executing:</color>\n{cmd}");
        await RunProcessAsync(cmd);
        Directory.Delete(temppath, true);
    }
    private async void BuildAndTestWithRenderDoc()
    {
        EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder)) return;
        await builder.Build();
        TestLastBuildWithRenderDoc();
    }
    private Task RunProcessAsync(string cmd)
    {
        return Task.Run(() =>
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = $"/c \"{cmd}\"";
            p.Start();

            string results = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Close();

            Debug.Log($"<color=green>cmd results:</color>\n{results}");
        });
    }
    private string GetVRCExePath()
    {
        string path = SDKClientUtilities.GetSavedVRCInstallPath();
        if (string.IsNullOrEmpty(path))
            path = SDKClientUtilities.LoadRegistryVRCInstallPath();
        return path;
    }
    private void OnEnable() => Reload();
    [MenuItem("Renard/Test last build with RenderDoc")]
    private static void OpenWindow()
    {
        var window = GetWindow<RenderDocLauncher>();
        window.minSize = new Vector2(550, 200);
        window.titleContent = new GUIContent("RenderDoc Launcher");
        window.Show();
    }
}