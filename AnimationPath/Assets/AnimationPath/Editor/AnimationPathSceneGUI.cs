using System;
using UnityEngine;
using System.Collections;
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
    private static List<AnimationPathPoint> animationPoints;

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
        EditorGUILayout.ObjectField(contentGameObject, activeGameObject, typeof (GameObject), true);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(contentClip, activeAnimationClip, typeof(AnimationClip), false);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("222222"))
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(activeRootGameObject, activeAnimationClip, 0f);
                AnimationMode.EndSampling();
                AnimationMode.StopAnimationMode();
            }
        }
        if (GUILayout.Button("注册动画时间"))
        {
            AnimationWindowUtil.RegisterTimeChangeListener(OnAnimationWindowTimeChange);
        }
        if (GUILayout.Button("取消注册动画时间"))
        {
            AnimationWindowUtil.UnRegisterTimeChangeListener();
        }
    }

    private static void OnAnimationWindowTimeChange(float time)
    {
        Debug.Log(time);
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
        List<AnimationPathPoint> points = animationPoints;
        Handles.color = Color.green;
        int numPos = points.Count;
        if (pointShow)
        {
            for (int i = 0; i < numPos; i++)
            {
                AnimationPathPoint pathPoint = points[i];
                Vector3 position = GetWorldPosition(pathPoint.position);
                float pointHandleSize = HandleUtility.GetHandleSize(position) * 0.1f;
                Handles.Label(position, "  Point " + i);
                if (Handles.Button(position, Quaternion.identity, pointHandleSize, pointHandleSize, Handles.DotCap))
                {
                    if (Selection.activeGameObject != activeGameObject)
                    {
                        Selection.activeGameObject = activeGameObject;
                    }
                    AnimationWindowUtil.SetCurrentTime(pathPoint.time);
                }
            }
        }

        for (int i = 0; i < numPos - 1; i++)
        {
            Vector3 startTangent;
            Vector3 endTangent;
            AnimationPathPoint.CalcTangents(points[i], points[i + 1], out startTangent, out endTangent);
            Handles.DrawBezier(GetWorldPosition(points[i].position), GetWorldPosition(points[i + 1].position), 
                GetWorldPosition(startTangent), GetWorldPosition(endTangent),
                Color.white, null, 2f);
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
        Debug.Log("onClipSelectionChanged");
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

    private static Vector3 GetWorldPosition(Vector3 localPosition)
    {
        if (activeRootGameObject != activeGameObject)
        {
            return activeRootGameObject.transform.TransformPoint(localPosition);
        }

        Transform parent = activeRootGameObject.transform.parent;
        if (parent == null)
        {
            return localPosition;
        }

        return parent.TransformPoint(localPosition);
    }
}
