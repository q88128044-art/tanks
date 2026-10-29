using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tanks
{
    /// <summary>
    /// Shows the controls hint, the number of enemies left and the result of
    /// the battle (victory / defeat). Press R to restart the scene.
    /// Added automatically to scenes that contain a player tank.
    /// </summary>
    public class BattleStatus : MonoBehaviour
    {
        private TankHealth _player;
        private TankHealth[] _enemies = new TankHealth[0];
        private GUIStyle _big;
        private GUIStyle _small;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<BattleStatus>() == null && GameObject.FindGameObjectWithTag("Player") != null)
            {
                new GameObject("BattleStatus").AddComponent<BattleStatus>();
            }
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _player = player != null ? player.GetComponent<TankHealth>() : null;

            System.Collections.Generic.List<TankHealth> enemies = new System.Collections.Generic.List<TankHealth>();
            foreach (AIController ai in FindObjectsByType<AIController>())
            {
                TankHealth h = ai.GetComponent<TankHealth>();
                if (h != null) enemies.Add(h);
            }

            _enemies = enemies.ToArray();
        }

        private int EnemiesAlive()
        {
            int alive = 0;
            foreach (TankHealth e in _enemies)
            {
                if (e != null && e.IsAlive) alive++;
            }

            return alive;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                    ? SceneManager.GetActiveScene().name
                    : SceneManager.GetActiveScene().path);
            }
        }

        private void OnGUI()
        {
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
                _small.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
            }

            float scale = Screen.height / 1080f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            string info = "WASD — рух   Миша — башта   ЛКМ/Space — постріл   1/2/3 — зброя   T — день/захід   R — рестарт";
            if (_enemies.Length > 0)
            {
                info = "Ворогів: " + EnemiesAlive() + "/" + _enemies.Length + "\n" + info;
            }

            GUI.Label(new Rect(width - 1020f, 16f, 1000f, 60f), info, _small);

            string result = null;
            Color color = Color.white;
            if (_player != null && !_player.IsAlive)
            {
                result = "ПОРАЗКА\n<size=26>натисни R, щоб почати знову</size>";
                color = new Color(1f, 0.35f, 0.3f);
            }
            else if (_enemies.Length > 0 && EnemiesAlive() == 0)
            {
                result = "ПЕРЕМОГА!\n<size=26>натисни R, щоб зіграти ще</size>";
                color = new Color(0.45f, 1f, 0.5f);
            }

            if (result != null)
            {
                _big.richText = true;
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.Label(new Rect(3f, 3f, width, 1080f), result, _big);
                GUI.color = color;
                GUI.Label(new Rect(0f, 0f, width, 1080f), result, _big);
                GUI.color = Color.white;
            }

            GUI.matrix = old;
        }
    }
}
