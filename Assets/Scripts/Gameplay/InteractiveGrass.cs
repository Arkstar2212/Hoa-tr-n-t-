using UnityEngine;

namespace HoaVaMau.Gameplay
{
    public enum GrassLayer
    {
        Back,
        Middle,
        Front,
        StealthReeds
    }

    /// <summary>
    /// [DEPRECATED] Legacy Procedural Spiky Grass Blade generator.
    /// Neutralized in favor of Stylized3DGrassTuft (volumetric painterly mesh tufts).
    /// Prevents 0-vertex or spiky meshes from ever being generated.
    /// </summary>
    [ExecuteAlways]
    public class InteractiveGrass : MonoBehaviour
    {
        [SerializeField] private GrassLayer grassLayer = GrassLayer.Middle;
        public GrassLayer Layer => grassLayer;

        private void Awake()
        {
            Neutralize();
        }

        private void Start()
        {
            Neutralize();
        }

        private void OnEnable()
        {
            Neutralize();
        }

        public void Setup(float width, float depth, int count, GrassLayer layer = GrassLayer.Middle)
        {
            grassLayer = layer;
            Neutralize();
        }

        public void BuildGrassMesh()
        {
            Neutralize();
        }

        private void Neutralize()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                mf.sharedMesh.Clear();
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = false;
            }
        }
    }
}
