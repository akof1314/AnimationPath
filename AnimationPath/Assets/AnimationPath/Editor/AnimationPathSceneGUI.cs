using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public static class AnimationPathSceneUI
{
    public static bool enabled;
    public static GameObject activeGameObject;
    public static AnimationClip activeAnimationClip;
    private static bool keepShow;
    private static bool pointShow = true;
    private static GameObject activeRootGameObject;
    private static Transform activeParentTransform;
    private static List<AnimationPathPoint> animationPoints;
    private static int selectedPointIndex;

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
            EditorGUILayout.ObjectField(contentGameObject, activeGameObject, typeof (GameObject), true);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginChangeCheck();
        keepShow = EditorGUILayout.Toggle(contentKeepShow, keepShow);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.RemoveAll(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
            if (keepShow)
            {
                SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Combine(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
            }
        }
        EditorGUI.BeginChangeCheck();
        pointShow = EditorGUILayout.Toggle(contentPointShow, pointShow);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(contentGameObject, activeGameObject, typeof (GameObject), true);
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

    private static void OpenSceneTool(GameObject go)
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
        AnimationWindowUtil.SetOnClipSelectionChanged(onClipSelectionChanged);
        AnimationUtility.onCurveWasModified += OnCurveWasModified;
        if (keepShow)
        {
            SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Combine(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
        }

        enabled = true;
        SceneView.RepaintAll();
    }

    private static void CloseSceneTool()
    {
        selectedPointIndex = -1;
        enabled = false;
        AnimationWindowUtil.SetOnClipSelectionChanged(onClipSelectionChanged, true);
        AnimationUtility.onCurveWasModified -= OnCurveWasModified;
        SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.RemoveAll(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(OnSceneViewGUI));
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

        if (curveX == null || curveY == null || curveZ == null)
        {
            Debug.LogError(activeGameObject.name + " 必须要有完整的 Position 动画曲线！");
            return false;
        }

        animationPoints = AnimationPathPoint.MakePoints(curveX, curveY, curveZ);
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
            if (Handles.Button(position, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
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
                if (Handles.Button(pathPoint.worldInTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
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
                if (Handles.Button(pathPoint.worldOutTangent, handleRotation, pointHandleSize, pointPickSize, Handles.DotCap))
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

    private static void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType deleted)
    {
        if (!enabled && activeAnimationClip != clip)
        {
            return;
        }

        //todo 判断binding是不是想要的

        InitPointsInfo();
    }

    private static void onClipSelectionChanged()
    {
        if (enabled && !keepShow)
        {
            activeAnimationClip = AnimationWindowUtil.GetActiveAnimationClip();

            AnimationClip[] clips = AnimationUtility.GetAnimationClips(activeRootGameObject);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == activeAnimationClip)
                {
                    InitPointsInfo();
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
            return false;
        }

        AnimationPathPoint.ModifyPointTangent(pathPoint, nextPathPoint, offset, isInTangent, curveX, curveY, curveZ);

        Undo.RegisterCompleteObjectUndo(activeAnimationClip, "Edit Curve");
        AnimationUtility.SetEditorCurve(activeAnimationClip, bindingX, curveX);
        AnimationUtility.SetEditorCurve(activeAnimationClip, bindingY, curveY);
        AnimationUtility.SetEditorCurve(activeAnimationClip, bindingZ, curveZ);
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
