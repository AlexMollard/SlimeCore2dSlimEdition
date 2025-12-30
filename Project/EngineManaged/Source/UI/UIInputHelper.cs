using System;
using System.Runtime.InteropServices;

namespace EngineManaged.UI;

internal static class UIInputHelper
{
    public static (float x, float y) ScreenToUIWorld(float screenX, float screenY, float uiHeight = 18.0f)
    {
        NativeMethods.Input_GetViewportRect(out int vx, out int vy, out int vw, out int vh);
        
        float vpW = vw > 0 ? (float)vw : 1920.0f;
        float vpH = vh > 0 ? (float)vh : 1080.0f;
        float aspect = (vpH > 0.0f) ? (vpW / vpH) : (16.0f / 9.0f);

        float uiWidth = uiHeight * aspect;

        float uiX = (screenX / vpW) * uiWidth - (uiWidth * 0.5f);
        float uiY = (uiHeight * 0.5f) - (screenY / vpH) * uiHeight;

        return (uiX, uiY);
    }
}
