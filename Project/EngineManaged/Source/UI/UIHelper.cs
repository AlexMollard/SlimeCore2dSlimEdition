using System.Runtime.InteropServices;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public static class UIHelper
{
    public static (float left, float right, float top, float bottom) GetScreenBounds()
    {
        NativeMethods.Input_GetViewportRect(out int vx, out int vy, out int vw, out int vh);
        
        // Handle minimized or invalid viewport
        if (vh <= 0) vh = 1; 
        
        float aspect = (float)vw / vh;
        
        float camSize = 20.0f; 
        
        // Try to find the primary camera
        foreach (var entity in Scene.Scene.Enumerate())
        {
             if (entity.HasComponent<CameraComponent>())
             {
                 var cam = entity.GetComponent<CameraComponent>();
                 if (cam.IsPrimary) 
                 {
                     camSize = cam.Size;
                     break;
                 }
             }
        }
        
        float halfHeight = camSize / 2.0f;
        float halfWidth = halfHeight * aspect;
        
        // Return world coordinates of screen edges (assuming camera is at 0,0)
        // If camera moves, UI typically moves with it or is drawn in screen space overlay.
        // For "World Space UI" that acts as HUD, it assumes Camera is at 0,0 or we add Camera Pos.
        
        return (-halfWidth, halfWidth, halfHeight, -halfHeight);
    }
}
