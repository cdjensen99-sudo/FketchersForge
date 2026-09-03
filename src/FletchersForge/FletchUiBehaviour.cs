using UnityEngine;

namespace FletchersForge;

internal sealed class FletchUiBehaviour : MonoBehaviour
{
    private void Update()
    {
        QuiverHud.Update();
        QuiverBackVisual.UpdateAll();
    }

    private void OnGUI()
    {
        FletchUiService.DrawBenchOverlay();
    }
}
