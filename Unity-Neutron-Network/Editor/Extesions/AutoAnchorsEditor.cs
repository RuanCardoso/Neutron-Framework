using UnityEngine;
using UnityEditor;
using System.Linq;

public class AutoAnchorsEditor : Editor
{
    private static void Anchor(RectTransform rectTrans)
    {
        RectTransform parentRectTrans = null;
        if (rectTrans.transform.parent != null)
            parentRectTrans = rectTrans.transform.parent.GetComponent<RectTransform>();

        if (parentRectTrans == null)
            return;

        Undo.RecordObject(rectTrans, "Auto Anchor Object");
        Rect pRect = parentRectTrans.rect;
        Vector2 min = new Vector2(rectTrans.anchorMin.x + (rectTrans.offsetMin.x / pRect.width), rectTrans.anchorMin.y + (rectTrans.offsetMin.y / pRect.height));
        Vector2 max = new Vector2(rectTrans.anchorMax.x + (rectTrans.offsetMax.x / pRect.width), rectTrans.anchorMax.y + (rectTrans.offsetMax.y / pRect.height));
        rectTrans.anchorMin = min;
        rectTrans.anchorMax = max;
        rectTrans.offsetMin = Vector2.zero;
        rectTrans.offsetMax = Vector2.zero;
        Vector2 centerPivot = new Vector2(0.5f, 0.5f);
        rectTrans.pivot = centerPivot;
        rectTrans.pivot = centerPivot;
    }

    [MenuItem("Neutron/UI Tools/Auto Anchors On Selected Game Objects")]
    private static void AnchorSelectedObjects()
    {
        RectTransform[] rectTransforms = Selection.gameObjects.Select(x => x.GetComponent<RectTransform>()).ToArray();
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTrans = rectTransforms[i];
            if (rectTrans != null)
                Anchor(rectTrans);
        }
    }

    [MenuItem("Neutron/UI Tools/Auto Anchors On All Game Objects")]
    private static void AnchorAll()
    {
        RectTransform[] rectTransforms = GameObject.FindObjectsOfType<RectTransform>();
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTrans = rectTransforms[i];
            if (rectTrans != null)
                Anchor(rectTrans);
        }
    }

    [MenuItem("Neutron/UI Tools/Auto Anchors On Selected Game Objects And Match")]
    private static void Match()
    {
        RectTransform[] rectTransforms = Selection.gameObjects.Select(x => x.GetComponent<RectTransform>()).ToArray();
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTrans = rectTransforms[i];
            if (rectTrans != null)
            {
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.anchoredPosition = Vector2.zero;
                rectTrans.sizeDelta = Vector3.zero;
                Anchor(rectTrans);
            }
        }
    }
}