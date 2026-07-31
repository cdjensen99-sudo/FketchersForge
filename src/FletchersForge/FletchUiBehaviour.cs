using UnityEngine;

namespace FletchersForge;

internal sealed class FletchUiBehaviour : MonoBehaviour
{
    private void OnGUI()
    {
        FletchUiService.DrawBenchOverlay();
    }
}
