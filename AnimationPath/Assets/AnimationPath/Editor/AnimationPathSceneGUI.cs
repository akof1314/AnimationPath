using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public static class AnimationPathSceneUI
{
    public static bool enabled;
    public static GameObject activeGameObject;
    public static AnimationClip activeAnimationClip;
    public static bool keepShow;
    private static bool pointShow = true;
    private static GameObject activeRootGameObject;
    private static Transform activeParentTransform;
    private static List<AnimationPathPoint> animationPoints;
    private static int selectedPointIndex;
    private static bool reloadPointsInfo;

    private static GUIContent contentTitle = new GUIContent("Animation Path");
    private static GUIContent contentOpen = new GUIContent("Enable Show Path");
    private static GUIContent contentClose = new GUIContent("Disable Show Path");
    private static GUIContent contentKeepShow = new GUIContent("Keep Show");
    private static GUIContent contentPointShow = new GUIContent("Point Show");
    private static GUIContent contentGameObject = new GUIContent("Anim Object");
    private static GUIContent contentClip = new GUIContent("Anim Clip");
    private static GUIContent contentOtherUse = new GUIContent("Other Anim Object have used.");

    public static void OnInspectorGUI(GameObject go)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(contentTitle);
        if (!enabled && GUILayout.Button(contentOpen))
        {
            OpenSceneTool(go);
        }

        if (!enabled)
        {
            return;
        }
        if (GUILayout.Button(contentClose))
        {
            CloseSceneTool();
        }
        EditorGUILayout.Space();

        if (go != activeGameObject)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(contentOtherUse);
            EditorGUILayout.ObjectField(contentGameObject, activeGameObject, typeof(GameObject), true);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginChangeCheck();
        keepShow = EditorGUILayout.Toggle(contentKeepShow, keepShow);
        if (EditorGUI.EndChangeCheck())
        {
            SetKeepShow(keepShow);
        }
        EditorGUI.BeginChangeCheck();
        pointShow = EditorGUILayout.Toggle(contentPointShow, pointShow);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(contentGameObject, activeGameObject, typeof(GameObject), true);
        EditorGUILayout.ObjectField(contentClip, activeAnimationClip, typeof(AnimationClip), false);
        EditorGUI.EndDisabledGroup();
    }

    public static void OnSceneGUI()
    {
        if (enabled && !keepShow)
        {
            DrawSceneViewGUI();
        }
    }

    public static void OpenSceneTool(GameObject go)
    {
        enabled = false;
        activeGameObject = go;
        selectedPointIndex = -1;

        CloseSceneTool();
        activeAnimationClip = AnimationWindowUtil.GetActiveAnimationClip();
        if (activeAnimationClip == null)
        {
            return;
        }

        InitPointsInfo();
        AnimationWindowUtil.SetOnFrameRateChange(onClipSelectionChanged);
        AnimationUtility.onCurveWasModified += OnCurveWasModified;
        if (keepShow)
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneViewGUI;
#else
            SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Combine(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
#endif
        }

        enabled = true;
        SceneView.RepaintAll();
    }

    private static void CloseSceneTool()
    {
        selectedPointIndex = -1;
        enabled = false;
        AnimationWindowUtil.SetOnFrameRateChange(onClipSelectionChanged, true);
        AnimationUtility.onCurveWasModified -= OnCurveWasModified;
        
        
#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui -= OnSceneViewGUI;
#else
        SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.RemoveAll(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
#endif
        SceneView.RepaintAll();
    }

    public static void SetKeepShow(bool show)
    {
        keepShow = show;
#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui -= OnSceneViewGUI;
        if (keepShow)
        {
            SceneView.duringSceneGui += OnSceneViewGUI;
        }
#else
        SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.RemoveAll(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
        if (keepShow)
        {
            SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Combine(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
        }
#endif
        SceneView.RepaintAll();
    }

    private static bool InitPointsInfo()
    {
        if (animationPoints == null)
        {
            animationPoints = new List<AnimationPathPoint>();
        }
        animationPoints.Clear();

        String inPath = String.Empty;
        activeRootGameObject = activeGameObject;
        if (activeGameObject.GetComponent<Animator>() == null && activeGameObject.GetComponent<Animation>() == null)
        {
            Transform tr = activeGameObject.transform.parent;
            while (!(tr.GetComponent<Animator>() != null))
            {
                if (tr == tr.root)
                {
                    return false;
                }
                tr = tr.parent;
            }
            activeRootGameObject = tr.gameObject;
            inPath = AnimationUtility.CalculateTransformPath(activeGameObject.transform, activeRootGameObject.transform);
        }
        activeParentTransform = activeGameObject.transform.parent;

        Type inType = typeof(Transform);
        AnimationCurve curveX = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.x"));
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.y"));
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.z"));
        Vector3 initPosition = activeRootGameObject.transform.localPosition;

        if (curveX == null || curveY == null || curveZ == null)
        {
            // 有可能是UI的动画
            var rt = activeRootGameObject.transform.GetComponent<RectTransform>();
            if (rt)
            {
                inType = typeof(RectTransform);
                curveX = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.x"));
                curveY = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.y"));
                curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip, EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.z"));
                initPosition = rt.anchoredPosition;

                if (curveX == null && curveY == null && curveZ == null)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        animationPoints = AnimationPathPoint.MakePoints(curveX, curveY, curveZ, initPosition);
        return true;
    }

    private static void OnSceneViewGUI(SceneView sceneView)
    {
        if (enabled && keepShow)
        {
            DrawSceneViewGUI();
        }
    }

    private static void DrawSceneViewGUI()
    {
        if (reloadPointsInfo)
        {
            reloadPointsInfo = false;
            int num = animationPoints.Count;
            InitPointsInfo();
            if (pointShow && animationPoints.Count > num)
            {
                // FIXME 这是为了修复新增点的时候，方向杆ID被改变了，所以操作无效
                // 不完美，需要第二次点击的时候，才会获取新控件ID
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
        }

        if (activeGameObject == null)
        {
            return;
        }

        List<AnimationPathPoint> points = animationPoints;
        int numPos = points.Count;
        for (int i = 0; i < numPos; i++)
        {
            AnimationPathPoint pathPoint = points[i];
            pathPoint.worldPosition = GetWorldPosition(pathPoint.position);
        }

        for (int i = 0; i < numPos - 1; i++)
        {
            AnimationPathPoint pathPoint = points[i];
            AnimationPathPoint nextPathPoint = points[i + 1];
            Vector3 startTangent;
            Vector3 endTangent;
            AnimationPathPoint.CalcTangents(pathPoint, nextPathPoint, out startTangent, out endTangent);

            Vector3 p0 = pathPoint.worldPosition;
            Vector3 p1 = GetWorldPosition(startTangent);
            Vector3 p2 = GetWorldPosition(endTangent);
            Vector3 p3 = nextPathPoint.worldPosition;

            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);

            pathPoint.worldOutTangent = p1;
            nextPathPoint.worldInTangent = p2;
        }

        if (!pointShow)
        {
            return;
        }

        Quaternion handleRotation = activeParentTransform != null
                ? activeParentTransform.rotation
                : Quaternion.identity;
        for (int i = 0; i < numPos; i++)
        {
            int pointIndex = i * 3;
            Handles.color = Color.green;
            AnimationPathPoint pathPoint = points[i];
            Vector3 position = pathPoint.worldPosition;
            float pointHandleSize = HandleUtility.GetHandleSize(position) * 0.04f;
            float pointPickSize = pointHandleSize * 0.7f;
            Handles.Label(position, " Point " + i);
#if UNITY_5_6_OR_NEWER
            if (Handles.Button(position, handleRotation, pointHandleSize, pointPickSize, Handles.DotHandleCap))
#else
            if (Handles.Button(position, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
#endif
            {
                selectedPointIndex = pointIndex;
                if (Selection.activeGameObject != activeGameObject)
                {
                    Selection.activeGameObject = activeGameObject;
                }
                AnimationWindowUtil.SetCurrentTime(pathPoint.time);
            }

            Handles.color = Color.grey;
            int inIndex = pointIndex - 1;
            int outIndex = pointIndex + 1;
            if (selectedPointIndex < 0 || selectedPointIndex < inIndex || selectedPointIndex > outIndex)
            {
                continue;
            }

            if (i != 0)
            {
                Handles.DrawLine(position, pathPoint.worldInTangent);
#if UNITY_5_6_OR_NEWER
                if (Handles.Button(pathPoint.worldInTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotHandleCap))
#else
                if (Handles.Button(pathPoint.worldInTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
#endif
                {
                    selectedPointIndex = inIndex;
                }

                if (selectedPointIndex == inIndex)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 pos = Handles.PositionHandle(pathPoint.worldInTangent, handleRotation);
                    if (EditorGUI.EndChangeCheck() && SetPointTangent(i, pos, true))
                    {
                        return;
                    }
                }
            }

            if (i != numPos - 1)
            {
                Handles.DrawLine(position, pathPoint.worldOutTangent);
#if UNITY_5_6_OR_NEWER
                if (Handles.Button(pathPoint.worldOutTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotHandleCap))
#else
                if (Handles.Button(pathPoint.worldOutTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
#endif
                {
                    selectedPointIndex = outIndex;
                }

                if (selectedPointIndex == outIndex)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 pos = Handles.PositionHandle(pathPoint.worldOutTangent, handleRotation);
                    if (EditorGUI.EndChangeCheck() && SetPointTangent(i, pos, false))
                    {
                        return;
                    }
                }
            }
        }
    }

    private static void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
    {
        if (!enabled && activeAnimationClip != clip)
        {
            return;
        }

        reloadPointsInfo = true;
    }

    private static void onClipSelectionChanged(float frameRate)
    {
        if (enabled && !keepShow)
        {
            activeAnimationClip = AnimationWindowUtil.GetActiveAnimationClip();

            AnimationClip[] clips = AnimationUtility.GetAnimationClips(activeRootGameObject);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == activeAnimationClip)
                {
                    reloadPointsInfo = true;
                    SceneView.RepaintAll();
                    return;
                }
            }
            CloseSceneTool();
        }
    }

    private static bool SetPointTangent(int pointIndex, Vector3 worldTangent, bool isInTangent)
    {
        List<AnimationPathPoint> points = animationPoints;
        AnimationPathPoint pathPoint = null;
        AnimationPathPoint nextPathPoint = null;
        Vector3 offset = Vector3.zero;
        if (isInTangent)
        {
            pathPoint = points[pointIndex - 1];
            nextPathPoint = points[pointIndex];

            offset = GetLocalPosition(worldTangent) - GetLocalPosition(nextPathPoint.worldInTangent);
        }
        else
        {
            pathPoint = points[pointIndex];
            nextPathPoint = points[pointIndex + 1];

            offset = GetLocalPosition(worldTangent) - GetLocalPosition(pathPoint.worldOutTangent);
        }

        string inPath = AnimationUtility.CalculateTransformPath(activeGameObject.transform, activeRootGameObject.transform);
        Type inType = typeof(Transform);
        EditorCurveBinding bindingX = EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.x");
        EditorCurveBinding bindingY = EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.y");
        EditorCurveBinding bindingZ = EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.z");

        AnimationCurve curveX = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingX);
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingY);
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingZ);

        if (curveX == null || curveY == null || curveZ == null)
        {
            var rt = activeRootGameObject.transform.GetComponent<RectTransform>();
            if (rt)
            {
                inType = typeof(RectTransform);
                bindingX = EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.x");
                bindingY = EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.y");
                bindingZ = EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.z");
                curveX = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingX);
                curveY = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingY);
                curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip, bindingZ);

                if (curveX == null && curveY == null && curveZ == null)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        AnimationPathPoint.ModifyPointTangent(pathPoint, nextPathPoint, offset, isInTangent, curveX, curveY, curveZ);

        Undo.RegisterCompleteObjectUndo(activeAnimationClip, "Edit Curve");
        if (curveX != null)
        {
            AnimationUtility.SetEditorCurve(activeAnimationClip, bindingX, curveX);
        }

        if (curveY != null)
        {
            AnimationUtility.SetEditorCurve(activeAnimationClip, bindingY, curveY);
        }

        if (curveZ != null)
        {
            AnimationUtility.SetEditorCurve(activeAnimationClip, bindingZ, curveZ);
        }
        AnimationWindowUtil.Repaint();

        return true;
    }

    private static Vector3 GetWorldPosition(Vector3 localPosition)
    {
        if (activeParentTransform == null)
        {
            return localPosition;
        }

        return activeParentTransform.TransformPoint(localPosition);
    }

    private static Vector3 GetLocalPosition(Vector3 worldPosition)
    {
        if (activeParentTransform == null)
        {
            return worldPosition;
        }

        return activeParentTransform.InverseTransformPoint(worldPosition);
    }
}
