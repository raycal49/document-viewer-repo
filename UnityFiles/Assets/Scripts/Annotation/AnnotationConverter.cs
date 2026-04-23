using Meta.XR.MRUtilityKit;
using System;
using UnityEngine;

public static class AnnotationConverter
{
    public static Vector3 ViewportToWorld(Camera cam, AnnotationPoint webPoint, float fallbackDepth = 2f)
    {
        if (cam == null)
            throw new ArgumentNullException(nameof(cam));

        float aspect = cam.aspect;
        if(cam.targetTexture != null)
            aspect = (float)cam.targetTexture.width / cam.targetTexture.height;
        
        Matrix4x4 proj = Matrix4x4.Perspective(cam.fieldOfView, aspect, cam.nearClipPlane, cam.farClipPlane);
    
        // Convert viewport point to a ray manually using the correct projection
        float viewX = webPoint.x * 2f - 1f;  
        float viewY = (1f - webPoint.y) * 2f - 1f;

        Matrix4x4 invVP = (proj * cam.worldToCameraMatrix).inverse;
        Vector4 nearPoint = invVP * new Vector4(viewX, viewY, -1f, 1f);
        Vector4 farPoint  = invVP * new Vector4(viewX, viewY,  1f, 1f);

        Vector3 nearW = new Vector3(nearPoint.x / nearPoint.w, nearPoint.y / nearPoint.w, nearPoint.z / nearPoint.w);
        Vector3 farW  = new Vector3(farPoint.x  / farPoint.w,  farPoint.y  / farPoint.w,  farPoint.z  / farPoint.w);

        Ray ray = new Ray(nearW, (farW - nearW).normalized);

        if (MRUK.Instance != null && MRUK.Instance.IsInitialized)
        {
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogWarning("MRUK room is null: using fallback depth");
            }
            if (room != null && room.Raycast(ray, 10f, new LabelFilter(), out RaycastHit hit))
            {
                return hit.point;
            }
        }
        
        return ray.GetPoint(fallbackDepth);
    }
}