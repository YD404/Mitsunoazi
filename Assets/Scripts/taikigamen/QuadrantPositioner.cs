using UnityEngine;

public static class QuadrantPositioner
{
    public enum ScreenQuadrant
    {
        TopRight,
        BottomRight,
        BottomLeft,
        TopLeft
    }

    public static Vector3 GetPosition(ScreenQuadrant quadrant, Camera camera, float zPos = 0f)
    {
        if (camera == null)
        {
            Debug.LogError("??????????????");
            return Vector3.zero;
        }

        Vector2 viewportPosition = Vector2.zero;

        switch (quadrant)
        {
            case ScreenQuadrant.TopRight:
                viewportPosition = new Vector2(0.75f, 0.75f);
                break;
            case ScreenQuadrant.BottomRight:
                viewportPosition = new Vector2(0.75f, 0.25f);
                break;
            case ScreenQuadrant.BottomLeft:
                viewportPosition = new Vector2(0.25f, 0.25f);
                break;
            case ScreenQuadrant.TopLeft:
                viewportPosition = new Vector2(0.25f, 0.75f);
                break;
        }

        Vector3 targetWorldPosition = camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, camera.nearClipPlane));
        targetWorldPosition.z = zPos;
        return targetWorldPosition;
    }
}