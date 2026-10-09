using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
    public class VFXSystem : MonoBehaviour
    {
        public static VFXSystem Instance { get; private set; }

        [Header("Pool")]
        [Range(4, 40)] [SerializeField] private int _poolSize = 16;

        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly List<VFXItem> _active = new List<VFXItem>();

        private sealed class VFXItem
        {
            public GameObject go;
            public float despawnAt;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Object.DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root);
        }

        private void Start()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject go = CreateVFXObject("VFX_" + i);
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                _pool.Add(go);
            }
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (now >= _active[i].despawnAt)
                {
                    ReturnToPool(_active[i].go);
                    _active.RemoveAt(i);
                }
            }
        }

        public void PlayMuzzleFlash(Vector3 position, Quaternion rotation, float size, float duration, Color color)
        {
            GameObject go = Rent();
            go.name = "MuzzleFlash";
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * size;
            SetColor(go, color);
            Schedule(go, duration);
        }

        public void PlayImpact(Vector3 position, Quaternion rotation, float size, float duration, Color color)
        {
            GameObject go = Rent();
            go.name = "Impact";
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * size;
            SetColor(go, color);
            Schedule(go, duration);
        }

        public void PlayExplosion(Vector3 position, float size, float duration, Color color)
        {
            GameObject go = Rent();
            go.name = "Explosion";
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = Vector3.one * size;
            SetColor(go, color);
            Schedule(go, duration);
        }

        private GameObject Rent()
        {
            foreach (GameObject go in _pool)
            {
                if (!go.activeInHierarchy)
                {
                    return go;
                }
            }

            GameObject newGo = CreateVFXObject("VFX");
            newGo.transform.SetParent(transform, false);
            newGo.SetActive(false);
            _pool.Add(newGo);
            return newGo;
        }

        private void SetColor(GameObject go, Color color)
        {
            Renderer[] renderers = go.GetComponentsInParent<Renderer>();
            foreach (Renderer item in renderers)
            {
                if (item != null)
                {
                    item.material.color = color;
                }
            }
        }

        private void Schedule(GameObject go, float duration)
        {
            go.transform.parent = transform;
            go.SetActive(true);
            _active.Add(new VFXItem { go = go, despawnAt = Time.time + duration });
        }

        private void ReturnToPool(GameObject go)
        {
            go.transform.parent = transform;
            go.SetActive(false);
        }

        private static GameObject CreateVFXObject(string name)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            if (go.TryGetComponent(out Collider col))
            {
                Destroy(col);
            }
            return go;
        }
    }
}