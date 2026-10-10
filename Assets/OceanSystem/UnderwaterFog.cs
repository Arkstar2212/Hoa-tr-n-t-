
using UnityEngine;

public class UnderwaterFog : MonoBehaviour
{
    public BoxCollider waterBody;

    private bool originalFog;

    void Start()
    {
        originalFog = RenderSettings.fog;
    }

    void LateUpdate()
    {
        if (waterBody == null) return;

        bool underwater =
            waterBody.bounds.Contains(transform.position);

        RenderSettings.fog = underwater;
    }

    void OnDisable()
    {
        RenderSettings.fog = originalFog;
    }
}
