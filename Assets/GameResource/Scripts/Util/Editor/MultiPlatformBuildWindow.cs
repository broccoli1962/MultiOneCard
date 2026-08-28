using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Backend.Editor
{
    /// <summary>
    /// Windows / Android / WebGL 을 차례로 전환해 빌드한다.
    /// 전환 시 도메인 리로드가 나서 SessionState 큐로 이어 간다.
    /// </summary>
    public sealed class MultiPlatformBuildWindow : EditorWindow
    {
        private const string MenuPath = "Tools/OneTable/Build Windows + Mobile + Web";
        internal const string PrefWin = "OneTable.MultiBuild.Win";
        internal const string PrefAndroid = "OneTable.MultiBuild.Android";
        internal const string PrefWeb = "OneTable.MultiBuild.Web";
        private const string PrefDev = "OneTable.MultiBuild.Dev";
        private const string PrefOut = "OneTable.MultiBuild.Out";

        private bool _development;
        private string _outputRoot;
        private Vector2 _logScroll;

        [MenuItem(MenuPath, priority = 10)]
        public static void Open()
        {
            var window = GetWindow<MultiPlatformBuildWindow>("멀티 빌드");
            window.minSize = new Vector2(440, 420);
            window.Show();
        }

        private void OnEnable()
        {
            _development = EditorPrefs.GetBool(PrefDev, false);
            _outputRoot = EditorPrefs.GetString(PrefOut, DefaultOutputRoot());
            EditorApplication.update += RepaintIfRunning;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintIfRunning;
            EditorPrefs.SetBool(PrefDev, _development);
            if (!string.IsNullOrEmpty(_outputRoot))
            {
                EditorPrefs.SetString(PrefOut, _outputRoot);
            }
        }

        private void OnGUI()
        {
            var running = MultiPlatformBuildRunner.IsRunning;
            EditorGUI.BeginDisabledGroup(running);

            EditorGUILayout.LabelField("한 번에 빌드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "선택한 플랫폼을 순서대로 전환해 빌드합니다. 전환 때 에디터가 다시 로드되며 이 창은 이어서 진행합니다.\n"
                + "Windows 환경의 모바일은 Android 입니다. iOS 는 Mac 이 필요합니다.\n"
                + "WebGL 은 빌드 후 WebPlayer 와 web 폴더를 갱신합니다. MultiOneCard / MultiOneCard-web Pages 에 올리면 브라우저에서 실행됩니다.\n"
                + MultiPlatformBuildRunner.GitHubPagesUrl + "\n"
                + MultiPlatformBuildRunner.GitHubPagesUrlSource,
                MessageType.Info);

            DrawVersionRow();

            EditorGUILayout.LabelField("빌드 대상", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(SelectionSummary());

            _development = EditorGUILayout.ToggleLeft("Development Build", _development);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("출력 폴더");
            EditorGUILayout.BeginHorizontal();
            _outputRoot = EditorGUILayout.TextField(_outputRoot);
            if (GUILayout.Button("찾기", GUILayout.Width(56)))
            {
                var picked = EditorUtility.OpenFolderPanel("빌드 출력", _outputRoot, string.Empty);
                if (!string.IsNullOrEmpty(picked))
                {
                    _outputRoot = picked;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            if (GUILayout.Button(running ? "빌드 중…" : "플랫폼 선택하고 빌드", GUILayout.Height(32)))
            {
                OpenSelectWindow();
            }

            EditorGUI.EndDisabledGroup();

            if (running && GUILayout.Button("다음 플랫폼부터 취소", GUILayout.Height(22)))
            {
                MultiPlatformBuildRunner.RequestCancel();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("로그", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(MultiPlatformBuildRunner.LogText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static void DrawVersionRow()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("버전", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                PlayerSettings.bundleVersion + "  (Android " + PlayerSettings.Android.bundleVersionCode + ")");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("마이너 업데이트", GUILayout.Height(24)))
            {
                ApplyVersionBump(minor: true);
            }

            if (GUILayout.Button("메이저 업데이트", GUILayout.Height(24)))
            {
                ApplyVersionBump(minor: false);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private static void ApplyVersionBump(bool minor)
        {
            var before = PlayerSettings.bundleVersion;
            var after = minor ? BumpMinor(before) : BumpMajor(before);
            PlayerSettings.bundleVersion = after;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode + 1);

            var ios = PlayerSettings.iOS.buildNumber;
            if (int.TryParse(ios, out var iosCode))
            {
                PlayerSettings.iOS.buildNumber = (iosCode + 1).ToString();
            }
            else
            {
                PlayerSettings.iOS.buildNumber = PlayerSettings.Android.bundleVersionCode.ToString();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[멀티 빌드] 버전 " + before + " → " + after
                + "  (Android " + PlayerSettings.Android.bundleVersionCode + ")");
        }

        private static string BumpMajor(string raw)
        {
            ParseVersion(raw, out var major, out _, out _, out var hasPatch);
            return FormatVersion(major + 1, 0, 0, hasPatch);
        }

        private static string BumpMinor(string raw)
        {
            ParseVersion(raw, out var major, out var minor, out _, out var hasPatch);
            return FormatVersion(major, minor + 1, 0, hasPatch);
        }

        private static void ParseVersion(string raw, out int major, out int minor, out int patch, out bool hasPatch)
        {
            major = 1;
            minor = 0;
            patch = 0;
            hasPatch = false;
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split('.');
            if (parts.Length > 0)
            {
                int.TryParse(parts[0], out major);
            }

            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out minor);
            }

            if (parts.Length > 2)
            {
                hasPatch = true;
                int.TryParse(parts[2], out patch);
            }
        }

        private static string FormatVersion(int major, int minor, int patch, bool hasPatch)
        {
            if (hasPatch)
            {
                return major + "." + minor + "." + patch;
            }

            return major + "." + minor;
        }

        internal static void DrawModuleHint(BuildTargetGroup group, BuildTarget target, string label)
        {
            if (BuildPipeline.IsBuildTargetSupported(group, target))
            {
                return;
            }

            EditorGUILayout.HelpBox(
                label + " 모듈이 설치되어 있지 않습니다. Unity Hub > Installs > Add modules.",
                MessageType.Warning);
        }

        private void OpenSelectWindow()
        {
            if (MultiPlatformBuildRunner.IsRunning)
            {
                return;
            }

            var root = string.IsNullOrWhiteSpace(_outputRoot) ? DefaultOutputRoot() : _outputRoot.Trim();
            EditorPrefs.SetString(PrefOut, root);
            EditorPrefs.SetBool(PrefDev, _development);
            MultiPlatformBuildSelectWindow.Open(root, _development);
        }

        internal static bool TryBeginBuild(List<string> ids, string outputRoot, bool development)
        {
            if (MultiPlatformBuildRunner.IsRunning)
            {
                return false;
            }

            if (ids == null || ids.Count == 0)
            {
                EditorUtility.DisplayDialog("멀티 빌드", "플랫폼을 하나 이상 선택하세요.", "확인");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            var root = string.IsNullOrWhiteSpace(outputRoot) ? DefaultOutputRoot() : outputRoot.Trim();
            EditorPrefs.SetString(PrefOut, root);
            EditorPrefs.SetBool(PrefDev, development);
            MultiPlatformBuildRunner.Start(ids, root, development);
            return true;
        }

        internal static string SelectionSummary()
        {
            var parts = new List<string>();
            if (EditorPrefs.GetBool(PrefWin, true))
            {
                parts.Add("Windows");
            }

            if (EditorPrefs.GetBool(PrefAndroid, true))
            {
                parts.Add("Mobile");
            }

            if (EditorPrefs.GetBool(PrefWeb, true))
            {
                parts.Add("Web");
            }

            return parts.Count == 0 ? "없음" : string.Join(", ", parts);
        }

        private void RepaintIfRunning()
        {
            if (MultiPlatformBuildRunner.IsRunning)
            {
                Repaint();
            }
        }

        private static string DefaultOutputRoot()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
        }
    }

    /// <summary>
    /// 빌드할 플랫폼만 고른다. 선택하지 않은 대상은 큐에 넣지 않는다.
    /// </summary>
    public sealed class MultiPlatformBuildSelectWindow : EditorWindow
    {
        private bool _windows;
        private bool _android;
        private bool _web;
        private string _outputRoot;
        private bool _development;

        public static void Open(string outputRoot, bool development)
        {
            var window = GetWindow<MultiPlatformBuildSelectWindow>(true, "빌드 선택", true);
            window.minSize = new Vector2(380, 280);
            window._outputRoot = outputRoot;
            window._development = development;
            window._windows = EditorPrefs.GetBool(MultiPlatformBuildWindow.PrefWin, true);
            window._android = EditorPrefs.GetBool(MultiPlatformBuildWindow.PrefAndroid, true);
            window._web = EditorPrefs.GetBool(MultiPlatformBuildWindow.PrefWeb, true);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("빌드할 플랫폼", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("체크한 플랫폼만 빌드합니다. 체크하지 않은 대상은 건너뜁니다.", MessageType.Info);

            _windows = EditorGUILayout.ToggleLeft("Windows (Standalone 64)", _windows);
            MultiPlatformBuildWindow.DrawModuleHint(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64,
                "Windows");
            _android = EditorGUILayout.ToggleLeft("Mobile (Android APK)", _android);
            MultiPlatformBuildWindow.DrawModuleHint(
                BuildTargetGroup.Android,
                BuildTarget.Android,
                "Android");
            _web = EditorGUILayout.ToggleLeft("Web (WebGL)", _web);
            MultiPlatformBuildWindow.DrawModuleHint(
                BuildTargetGroup.WebGL,
                BuildTarget.WebGL,
                "WebGL");

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 선택"))
            {
                _windows = true;
                _android = true;
                _web = true;
            }

            if (GUILayout.Button("선택 해제"))
            {
                _windows = false;
                _android = false;
                _web = false;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(28)))
            {
                Close();
            }

            if (GUILayout.Button("선택한 것만 빌드", GUILayout.Height(28)))
            {
                Confirm();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Confirm()
        {
            EditorPrefs.SetBool(MultiPlatformBuildWindow.PrefWin, _windows);
            EditorPrefs.SetBool(MultiPlatformBuildWindow.PrefAndroid, _android);
            EditorPrefs.SetBool(MultiPlatformBuildWindow.PrefWeb, _web);

            var ids = new List<string>();
            if (_windows)
            {
                ids.Add(MultiPlatformBuildRunner.IdWindows);
            }

            if (_android)
            {
                ids.Add(MultiPlatformBuildRunner.IdAndroid);
            }

            if (_web)
            {
                ids.Add(MultiPlatformBuildRunner.IdWeb);
            }

            if (MultiPlatformBuildWindow.TryBeginBuild(ids, _outputRoot, _development))
            {
                Close();
            }
        }
    }

    /// <summary>
    /// 플랫폼 전환 리로드 뒤에도 큐를 이어서 빌드한다.
    /// </summary>
    [InitializeOnLoad]
    internal static class MultiPlatformBuildRunner
    {
        public const string IdWindows = "Windows";
        public const string IdAndroid = "Android";
        public const string IdWeb = "WebGL";
        public const string GitHubPagesUrl = "https://broccoli1962.github.io/MultiOneCard-web/";
        public const string GitHubPagesUrlSource = "https://broccoli1962.github.io/MultiOneCard/";
        private const string GitHubWebFolder = "web";
        private const string GitHubPlayerFolder = "WebPlayer";

        [MenuItem("Tools/OneTable/Prepare GitHub Web Player", priority = 11)]
        public static void PrepareGitHubWebPlayerMenu()
        {
            var src = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "WebGL");
            if (!File.Exists(Path.Combine(src, "index.html")))
            {
                EditorUtility.DisplayDialog(
                    "GitHub Web",
                    "Builds/WebGL/index.html 이 없습니다. 먼저 WebGL 을 빌드하세요.",
                    "확인");
                return;
            }

            try
            {
                var published = PublishWebPlayer(src);
                EditorUtility.RevealInFinder(published);
                EditorUtility.DisplayDialog(
                    "GitHub Web",
                    "WebPlayer 는 MultiOneCard Pages, web 은 MultiOneCard-web 에 푸시하면 실행됩니다.\n\n"
                    + GitHubPagesUrl + "\n"
                    + GitHubPagesUrlSource,
                    "확인");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("GitHub Web", e.Message, "확인");
            }
        }

        private const string KeyRunning = "OneTable.MultiBuild.Running";
        private const string KeyQueue = "OneTable.MultiBuild.Queue";
        private const string KeyLog = "OneTable.MultiBuild.Log";
        private const string KeyDev = "OneTable.MultiBuild.RunDev";
        private const string KeyOut = "OneTable.MultiBuild.RunOut";
        private const string KeyRestore = "OneTable.MultiBuild.Restore";
        private const string KeyCancel = "OneTable.MultiBuild.Cancel";

        static MultiPlatformBuildRunner()
        {
            EditorApplication.delayCall += Resume;
        }

        public static bool IsRunning => SessionState.GetBool(KeyRunning, false);

        public static string LogText => SessionState.GetString(KeyLog, string.Empty);

        public static void Start(List<string> ids, string outputRoot, bool development)
        {
            SessionState.SetBool(KeyRunning, true);
            SessionState.SetBool(KeyCancel, false);
            SessionState.SetString(KeyQueue, string.Join(",", ids));
            SessionState.SetString(KeyOut, outputRoot ?? string.Empty);
            SessionState.SetBool(KeyDev, development);
            SessionState.SetInt(KeyRestore, (int)EditorUserBuildSettings.activeBuildTarget);
            SessionState.SetString(KeyLog, string.Empty);
            Append("시작  " + string.Join(", ", ids));
            EditorApplication.delayCall += Step;
        }

        public static void RequestCancel()
        {
            SessionState.SetBool(KeyCancel, true);
            Append("다음 플랫폼부터 취소 요청");
        }

        private static void Resume()
        {
            if (!IsRunning)
            {
                return;
            }

            if (ReadQueue().Count == 0)
            {
                SessionState.SetBool(KeyRunning, false);
                return;
            }

            MultiPlatformBuildWindow.Open();
            EditorApplication.delayCall += Step;
        }

        private static void Step()
        {
            if (!IsRunning)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Step;
                return;
            }

            if (EditorApplication.isPlaying)
            {
                FailAndStop("플레이 모드에서는 빌드할 수 없습니다");
                return;
            }

            if (SessionState.GetBool(KeyCancel, false))
            {
                Finish("취소됨");
                return;
            }

            var queue = ReadQueue();
            if (queue.Count == 0)
            {
                Finish("완료");
                return;
            }

            var id = queue[0];
            if (!TryGetTarget(id, out var target, out var named))
            {
                Append(id + ": 알 수 없는 플랫폼, 건너뜀");
                PopQueue();
                EditorApplication.delayCall += Step;
                return;
            }

            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Append(id + ": 빌드 모듈 없음, 건너뜀");
                PopQueue();
                EditorApplication.delayCall += Step;
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Append(id + ": 플랫폼 전환");
                var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(named, target);
                if (!switched)
                {
                    Append(id + ": 전환 실패, 건너뜀");
                    PopQueue();
                    EditorApplication.delayCall += Step;
                    return;
                }

                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    return;
                }
            }

            try
            {
                EditorUtility.DisplayProgressBar("멀티 빌드", id + " Addressables", 0.35f);
                var addrError = BuildAddressables();
                if (!string.IsNullOrEmpty(addrError))
                {
                    Append(id + ": Addressables 실패  " + addrError);
                }
                else
                {
                    EditorUtility.DisplayProgressBar("멀티 빌드", id + " Player", 0.75f);
                    var playerError = BuildPlayer(id, target);
                    if (string.IsNullOrEmpty(playerError))
                    {
                        Append(id + ": 성공  " + OutputPath(id));
                        if (id == IdWeb)
                        {
                            try
                            {
                                var published = PublishWebPlayer(OutputPath(id));
                                Append(id + ": GitHub 웹 폴더  " + published + " / WebPlayer");
                            }
                            catch (Exception pubEx)
                            {
                                Append(id + ": GitHub 웹 폴더 실패  " + pubEx.Message);
                            }
                        }
                    }
                    else
                    {
                        Append(id + ": 실패  " + playerError);
                    }
                }
            }
            catch (Exception e)
            {
                Append(id + ": 예외  " + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            PopQueue();
            EditorApplication.delayCall += Step;
        }

        private static void Finish(string reason)
        {
            SessionState.SetBool(KeyRunning, false);
            SessionState.SetBool(KeyCancel, false);
            EditorUtility.ClearProgressBar();
            Append(reason);

            var restore = (BuildTarget)SessionState.GetInt(
                KeyRestore,
                (int)BuildTarget.StandaloneWindows64);
            var output = SessionState.GetString(KeyOut, string.Empty);
            if (!string.IsNullOrEmpty(output) && Directory.Exists(output))
            {
                EditorUtility.RevealInFinder(output);
            }

            EditorUtility.DisplayDialog("멀티 빌드", reason + "\n\n" + TrimForDialog(LogText), "확인");

            if (EditorUserBuildSettings.activeBuildTarget != restore)
            {
                var group = BuildPipeline.GetBuildTargetGroup(restore);
                var named = NamedBuildTarget.FromBuildTargetGroup(group);
                Append("원래 플랫폼으로 복귀  " + restore);
                EditorUserBuildSettings.SwitchActiveBuildTarget(named, restore);
            }
        }

        private static void FailAndStop(string message)
        {
            Append(message);
            Finish("중단");
        }

        private static string BuildAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return "AddressableAssetSettings 가 없습니다";
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (result != null && !string.IsNullOrEmpty(result.Error))
            {
                return result.Error;
            }

            return null;
        }

        private static string BuildPlayer(string id, BuildTarget target)
        {
            var scenes = EnabledScenePaths();
            if (scenes.Length == 0)
            {
                return "Build Settings 에 활성화된 씬이 없습니다";
            }

            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = false;
            }

            var location = OutputPath(id);
            EnsureOutputDirectory(id, location);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = SessionState.GetBool(KeyDev, false)
                    ? BuildOptions.Development
                    : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                return null;
            }

            return report.summary.result.ToString();
        }

        private static string[] EnabledScenePaths()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new List<string>();
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled && !string.IsNullOrEmpty(scenes[i].path))
                {
                    paths.Add(scenes[i].path);
                }
            }

            return paths.ToArray();
        }

        private static void EnsureOutputDirectory(string id, string location)
        {
            if (id == IdWeb)
            {
                Directory.CreateDirectory(location);
                return;
            }

            var dir = Path.GetDirectoryName(location);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string OutputPath(string id)
        {
            var root = SessionState.GetString(KeyOut, string.Empty);
            var product = Sanitize(PlayerSettings.productName);
            switch (id)
            {
                case IdWindows:
                    return Path.Combine(root, "Windows64", product + ".exe");
                case IdAndroid:
                    return Path.Combine(root, "Android", product + ".apk");
                default:
                    return Path.Combine(root, "WebGL");
            }
        }

        private static bool TryGetTarget(string id, out BuildTarget target, out NamedBuildTarget named)
        {
            switch (id)
            {
                case IdWindows:
                    target = BuildTarget.StandaloneWindows64;
                    named = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
                    return true;
                case IdAndroid:
                    target = BuildTarget.Android;
                    named = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android);
                    return true;
                case IdWeb:
                    target = BuildTarget.WebGL;
                    named = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.WebGL);
                    return true;
                default:
                    target = BuildTarget.NoTarget;
                    named = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
                    return false;
            }
        }

        private static List<string> ReadQueue()
        {
            var raw = SessionState.GetString(KeyQueue, string.Empty);
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw))
            {
                return list;
            }

            var parts = raw.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.Length > 0)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        private static void PopQueue()
        {
            var queue = ReadQueue();
            if (queue.Count > 0)
            {
                queue.RemoveAt(0);
            }

            SessionState.SetString(KeyQueue, string.Join(",", queue));
        }

        private static void Append(string line)
        {
            var stamp = DateTime.Now.ToString("HH:mm:ss");
            var next = LogText;
            if (next.Length > 0)
            {
                next += "\n";
            }

            next += stamp + "  " + line;
            if (next.Length > 8000)
            {
                next = next.Substring(next.Length - 8000);
            }

            SessionState.SetString(KeyLog, next);
            Debug.Log("[멀티 빌드] " + line);
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Game";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                var ok = true;
                for (var j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        ok = false;
                        break;
                    }
                }

                sb.Append(ok ? c : '_');
            }

            return sb.ToString();
        }

        private static string TrimForDialog(string log)
        {
            if (string.IsNullOrEmpty(log) || log.Length <= 1200)
            {
                return log ?? string.Empty;
            }

            return log.Substring(log.Length - 1200);
        }

        /// <summary>
        /// GitHub Pages 는 gzip Content-Encoding 을 붙이지 않으므로 .gz 를 풀어
        /// web(MultiOneCard-web) 과 WebPlayer(이 저장소 Pages) 에 복사한다.
        /// </summary>
        internal static string PublishWebPlayer(string webGlOutputDir)
        {
            if (string.IsNullOrEmpty(webGlOutputDir) || !Directory.Exists(webGlOutputDir))
            {
                throw new DirectoryNotFoundException(webGlOutputDir ?? "WebGL");
            }

            var root = Directory.GetParent(Application.dataPath).FullName;
            var webDest = Path.Combine(root, GitHubWebFolder);
            PublishWebPlayerTo(webGlOutputDir, webDest, keepGitMetadata: true);
            PublishWebPlayerTo(
                webGlOutputDir,
                Path.Combine(root, GitHubPlayerFolder),
                keepGitMetadata: false);
            return webDest;
        }

        private static void PublishWebPlayerTo(string webGlOutputDir, string dest, bool keepGitMetadata)
        {
            if (Directory.Exists(dest))
            {
                if (keepGitMetadata)
                {
                    ClearDirectoryExceptGit(dest);
                }
                else
                {
                    ClearDirectoryAll(dest);
                }
            }

            CopyDirectory(webGlOutputDir, dest);
            DecompressGzipUnder(Path.Combine(dest, "Build"));
            RewriteIndexForUncompressed(Path.Combine(dest, "index.html"));
            File.WriteAllText(Path.Combine(dest, ".nojekyll"), string.Empty);
        }

        private static void ClearDirectoryAll(string dest)
        {
            var files = Directory.GetFiles(dest);
            for (var i = 0; i < files.Length; i++)
            {
                DeleteFileForce(files[i]);
            }

            var dirs = Directory.GetDirectories(dest);
            for (var i = 0; i < dirs.Length; i++)
            {
                DeleteDirectoryForce(dirs[i]);
            }
        }

        private static void ClearDirectoryExceptGit(string dest)
        {
            var files = Directory.GetFiles(dest);
            for (var i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileName(files[i]);
                if (name == ".gitattributes" || name == ".gitignore" || name == "README.md")
                {
                    continue;
                }

                DeleteFileForce(files[i]);
            }

            var dirs = Directory.GetDirectories(dest);
            for (var i = 0; i < dirs.Length; i++)
            {
                if (Path.GetFileName(dirs[i]) == ".git")
                {
                    continue;
                }

                DeleteDirectoryForce(dirs[i]);
            }
        }

        private static void DeleteDirectoryForce(string path)
        {
            var files = Directory.GetFiles(path);
            for (var i = 0; i < files.Length; i++)
            {
                DeleteFileForce(files[i]);
            }

            var dirs = Directory.GetDirectories(path);
            for (var i = 0; i < dirs.Length; i++)
            {
                DeleteDirectoryForce(dirs[i]);
            }

            Directory.Delete(path, false);
        }

        private static void DeleteFileForce(string path)
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        private static void CopyDirectory(string src, string dest)
        {
            Directory.CreateDirectory(dest);
            var files = Directory.GetFiles(src);
            for (var i = 0; i < files.Length; i++)
            {
                File.Copy(files[i], Path.Combine(dest, Path.GetFileName(files[i])), true);
            }

            var dirs = Directory.GetDirectories(src);
            for (var i = 0; i < dirs.Length; i++)
            {
                var name = Path.GetFileName(dirs[i]);
                if (name == ".git")
                {
                    continue;
                }

                CopyDirectory(dirs[i], Path.Combine(dest, name));
            }
        }

        private static void DecompressGzipUnder(string buildDir)
        {
            if (!Directory.Exists(buildDir))
            {
                return;
            }

            var gzFiles = Directory.GetFiles(buildDir, "*.gz");
            for (var i = 0; i < gzFiles.Length; i++)
            {
                var gz = gzFiles[i];
                var raw = gz.Substring(0, gz.Length - 3);
                using (var input = File.OpenRead(gz))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                using (var output = File.Create(raw))
                {
                    gzip.CopyTo(output);
                }

                File.Delete(gz);
            }
        }

        private static void RewriteIndexForUncompressed(string indexPath)
        {
            if (!File.Exists(indexPath))
            {
                return;
            }

            var html = File.ReadAllText(indexPath);
            html = html.Replace(".data.gz", ".data");
            html = html.Replace(".framework.js.gz", ".framework.js");
            html = html.Replace(".wasm.gz", ".wasm");
            html = html.Replace(".js.gz", ".js");
            if (html.IndexOf("<base ", StringComparison.OrdinalIgnoreCase) < 0)
            {
                html = html.Replace("<head>", "<head>\n    <base href=\"./\">");
            }

            File.WriteAllText(indexPath, html);
        }
    }
}
