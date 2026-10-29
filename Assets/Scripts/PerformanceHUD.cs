using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tanks
{
    /// <summary>
    /// FPS / frame-time overlay with renderer stats (draw calls, batches,
    /// SetPass calls, triangles) and memory.
    /// F3 - toggle compact / detailed / hidden.
    /// F4 - 10-second benchmark: prints avg FPS, 1% low and worst frame to the Console.
    /// Added automatically to every scene.
    /// </summary>
    public class PerformanceHUD : MonoBehaviour
    {
        private const int HistorySize = 600;

        private enum Mode { Compact, Detailed, Hidden }

        private Mode _mode = Mode.Compact;
        private readonly float[] _history = new float[HistorySize];
        private int _historyIndex;
        private int _historyCount;

        private float _accumTime;
        private int _accumFrames;
        private float _worstInWindow;
        private float _fps;
        private float _avgMs;
        private float _worstMs;
        private float _low1Fps;

        private bool _benchmarking;
        private float _benchmarkEnd;
        private readonly System.Collections.Generic.List<float> _benchmarkFrames = new System.Collections.Generic.List<float>(4096);
        private string _lastReport;

        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _srpDrawCalls;
        private ProfilerRecorder _instancedDrawCalls;
        private ProfilerRecorder _brgDrawCalls;
        private ProfilerRecorder _staticBatches;
        private ProfilerRecorder _instancedBatches;
        private ProfilerRecorder _srpInstances;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;
        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcMemory;
        private ProfilerRecorder _systemMemory;

        private string _text = "";
        private GUIStyle _style;
        private Texture2D _background;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<PerformanceHUD>() == null)
            {
                new GameObject("PerformanceHUD").AddComponent<PerformanceHUD>();
            }
        }

        private void OnEnable()
        {
            // Unity 6.x splits draw calls by path; older versions expose "Draw Calls Count".
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Standard Draw Calls Count");
            if (!_drawCalls.Valid) _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _srpDrawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SRP Batcher Draw Calls Count");
            _instancedDrawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Standard Instanced Draw Calls Count");
            _brgDrawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "BRG Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _staticBatches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batches Count");
            _instancedBatches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Instanced Batches Count");
            _srpInstances = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SRP Batcher Instances Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            _gcMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            _systemMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _batches.Dispose();
            _srpDrawCalls.Dispose();
            _instancedDrawCalls.Dispose();
            _brgDrawCalls.Dispose();
            _staticBatches.Dispose();
            _instancedBatches.Dispose();
            _srpInstances.Dispose();
            _setPass.Dispose();
            _triangles.Dispose();
            _vertices.Dispose();
            _mainThread.Dispose();
            _gcMemory.Dispose();
            _systemMemory.Dispose();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _history[_historyIndex] = dt;
            _historyIndex = (_historyIndex + 1) % HistorySize;
            _historyCount = Mathf.Min(_historyCount + 1, HistorySize);

            _accumTime += dt;
            _accumFrames++;
            _worstInWindow = Mathf.Max(_worstInWindow, dt);

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f3Key.wasPressedThisFrame)
                {
                    _mode = (Mode)(((int)_mode + 1) % 3);
                }

                if (keyboard.f4Key.wasPressedThisFrame && !_benchmarking)
                {
                    _benchmarking = true;
                    _benchmarkEnd = Time.unscaledTime + 10f;
                    _benchmarkFrames.Clear();
                    Debug.Log("[Perf] Benchmark started (10 s)...");
                }
            }

            if (_benchmarking)
            {
                _benchmarkFrames.Add(dt);
                if (Time.unscaledTime >= _benchmarkEnd)
                {
                    _benchmarking = false;
                    _lastReport = BuildBenchmarkReport();
                    Debug.Log(_lastReport);
                }
            }

            if (_accumTime >= 0.5f)
            {
                _fps = _accumFrames / _accumTime;
                _avgMs = _accumTime / _accumFrames * 1000f;
                _worstMs = _worstInWindow * 1000f;
                _low1Fps = ComputeLowFps(0.01f);
                _accumTime = 0f;
                _accumFrames = 0;
                _worstInWindow = 0f;
                RebuildText();
            }
        }

        private float ComputeLowFps(float fraction)
        {
            if (_historyCount < 10) return _fps;
            float[] copy = new float[_historyCount];
            System.Array.Copy(_history, copy, _historyCount);
            System.Array.Sort(copy);
            int count = Mathf.Max(1, Mathf.RoundToInt(_historyCount * fraction));
            float sum = 0f;
            for (int i = _historyCount - count; i < _historyCount; i++) sum += copy[i];
            return count / Mathf.Max(0.0001f, sum);
        }

        private string BuildBenchmarkReport()
        {
            int n = _benchmarkFrames.Count;
            if (n == 0) return "[Perf] Benchmark: no frames";
            float total = 0f;
            foreach (float f in _benchmarkFrames) total += f;
            float[] sorted = _benchmarkFrames.ToArray();
            System.Array.Sort(sorted);
            int lowCount = Mathf.Max(1, n / 100);
            float lowSum = 0f;
            for (int i = n - lowCount; i < n; i++) lowSum += sorted[i];

            StringBuilder sb = new StringBuilder();
            sb.Append("[Perf] Benchmark 10 s | scene ").Append(SceneManager.GetActiveScene().name);
            sb.Append(" | ").Append(Screen.width).Append("x").Append(Screen.height);
            sb.Append(" | quality ").Append(QualitySettings.names[QualitySettings.GetQualityLevel()]);
            sb.Append("\n  avg FPS: ").Append((n / total).ToString("0.0"));
            sb.Append("   1% low: ").Append((lowCount / lowSum).ToString("0.0"));
            sb.Append("   worst frame: ").Append((sorted[n - 1] * 1000f).ToString("0.0")).Append(" ms");
            sb.Append("   median: ").Append((sorted[n / 2] * 1000f).ToString("0.0")).Append(" ms");
            sb.Append("\n  draw calls: ").Append(DrawCalls).Append("   batches: ").Append(Batches).Append("   SRP instances: ").Append(Last(_srpInstances));
            sb.Append("   setpass: ").Append(Last(_setPass)).Append("   tris: ").Append(FormatCount(Last(_triangles)));
            sb.Append("\n  CPU main thread: ").Append((AverageNs(_mainThread) * 1e-6).ToString("0.0")).Append(" ms");
            sb.Append("   memory: ").Append((Last(_systemMemory) / (1024 * 1024)).ToString()).Append(" MB");
            return sb.ToString();
        }

        private static long Last(ProfilerRecorder r)
        {
            return r.Valid ? r.LastValue : -1;
        }

        private static long Sum(params ProfilerRecorder[] recorders)
        {
            long total = 0;
            bool any = false;
            foreach (ProfilerRecorder r in recorders)
            {
                if (!r.Valid) continue;
                any = true;
                total += r.LastValue;
            }

            return any ? total : -1;
        }

        private long DrawCalls => Sum(_drawCalls, _srpDrawCalls, _instancedDrawCalls, _brgDrawCalls);
        private long Batches => _batches.Valid && _batches.LastValue > 0 ? _batches.LastValue : Sum(_staticBatches, _instancedBatches, _srpDrawCalls, _drawCalls);

        private static double AverageNs(ProfilerRecorder r)
        {
            if (!r.Valid || r.Count == 0) return 0;
            double sum = 0;
            int count = r.Count;
            for (int i = 0; i < count; i++) sum += r.GetSample(i).Value;
            return sum / count;
        }

        private static string FormatCount(long v)
        {
            if (v < 0) return "n/a";
            if (v >= 1000000) return (v / 1000000f).ToString("0.00") + "M";
            if (v >= 1000) return (v / 1000f).ToString("0.0") + "k";
            return v.ToString();
        }

        private void RebuildText()
        {
            string color = _fps >= 60f ? "#7CFF7C" : _fps >= 30f ? "#FFE066" : "#FF6B6B";
            StringBuilder sb = new StringBuilder();
            sb.Append("<b><color=").Append(color).Append(">").Append(_fps.ToString("0")).Append(" FPS</color></b>");
            sb.Append("  ").Append(_avgMs.ToString("0.0")).Append(" ms");

            if (_mode == Mode.Detailed)
            {
                sb.Append("\n1% low: ").Append(_low1Fps.ToString("0")).Append(" FPS   worst: ").Append(_worstMs.ToString("0.0")).Append(" ms");
                sb.Append("\nCPU main: ").Append((AverageNs(_mainThread) * 1e-6).ToString("0.0")).Append(" ms");
                sb.Append("\nDraw calls: ").Append(FormatCount(DrawCalls)).Append("   Batches: ").Append(FormatCount(Batches));
                sb.Append("\nSRP Batcher: ").Append(FormatCount(Last(_srpDrawCalls))).Append(" calls / ").Append(FormatCount(Last(_srpInstances))).Append(" obj");
                sb.Append("\nSetPass: ").Append(FormatCount(Last(_setPass))).Append("   Tris: ").Append(FormatCount(Last(_triangles)));
                sb.Append("\nVerts: ").Append(FormatCount(Last(_vertices)));
                sb.Append("\nMemory: ").Append(Last(_systemMemory) / (1024 * 1024)).Append(" MB   GC: ").Append(Last(_gcMemory) / (1024 * 1024)).Append(" MB");
                sb.Append("\nShells: ").Append(Projectile.TotalActive);
                if (VFXSystem.Instance != null) sb.Append("   VFX: ").Append(VFXSystem.Instance.ActiveEffects).Append("   Decals: ").Append(VFXSystem.Instance.ActiveDecals);
                sb.Append("\n").Append(Screen.width).Append("x").Append(Screen.height).Append("  ").Append(QualitySettings.names[QualitySettings.GetQualityLevel()]);
                sb.Append("  vSync ").Append(QualitySettings.vSyncCount);
                sb.Append("\n<size=12>F3 — режим   F4 — бенчмарк 10 с</size>");
                if (_benchmarking) sb.Append("\n<color=#FFE066>Бенчмарк... ").Append(Mathf.CeilToInt(_benchmarkEnd - Time.unscaledTime)).Append(" с</color>");
                else if (_lastReport != null) sb.Append("\n<size=12>Звіт бенчмарку — у Console</size>");
            }
            else
            {
                sb.Append("  <size=11>(F3)</size>");
            }

            _text = sb.ToString();
        }

        private void OnGUI()
        {
            if (_mode == Mode.Hidden || string.IsNullOrEmpty(_text))
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, alignment = TextAnchor.UpperLeft };
                _style.normal.textColor = Color.white;
                _style.padding = new RectOffset(8, 8, 6, 6);
                _background = new Texture2D(1, 1);
                _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
                _background.Apply();
                _style.normal.background = _background;
            }

            Vector2 size = _style.CalcSize(new GUIContent(_text));
            GUI.Label(new Rect(Screen.width * 0.5f - size.x * 0.5f, 8f, size.x, size.y), _text, _style);
        }
    }
}
