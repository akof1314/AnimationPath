using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class AnimationWindowReflect
{
    private Assembly m_Assembly;
    private Type m_TypeAnimationWindow;
    private Type m_TypeAnimEditor;
    private Type m_TypeAnimationWindowState;
    private Type m_TypeAnimationWindowSelection;
    private EditorWindow m_FirstAnimationWindow;

    // 以下都是对第一个动画窗体的对象
    private object m_AnimEditor;
    private object m_AnimationWindowState;
    private object m_AnimationWindowSelection;
    private PropertyInfo m_recordingInfo;
    private PropertyInfo m_currentTimeInfo;
    private PropertyInfo m_activeRootGameObjectInfo;
    private PropertyInfo m_activeAnimationClipInfo;
    private FieldInfo m_onClipSelectionChangedInfo;
    private Func<float> m_CurrentTimeGetFunc;

    private Assembly assembly
    {
        get
        {
            if (m_Assembly == null)
            {
                m_Assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
            }
            return m_Assembly;
        }
    }

    private Type animationWindowType
    {
        get
        {
            if (m_TypeAnimationWindow == null)
            {
                m_TypeAnimationWindow = assembly.GetType("UnityEditor.AnimationWindow");
            }
            return m_TypeAnimationWindow;
        }
    }

    private Type animEditorType
    {
        get
        {
            if (m_TypeAnimEditor == null)
            {
                m_TypeAnimEditor = assembly.GetType("UnityEditor.AnimEditor");
            }
            return m_TypeAnimEditor;
        }
    }

    private Type animationWindowStateType
    {
        get
        {
            if (m_TypeAnimationWindowState == null)
            {
                m_TypeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
            }
            return m_TypeAnimationWindowState;
        }
    }

    private Type animationWindowSelectionType
    {
        get
        {
            if (m_TypeAnimationWindowSelection == null)
            {
                m_TypeAnimationWindowSelection = assembly.GetType("UnityEditorInternal.AnimationWindowSelection");
            }
            return m_TypeAnimationWindowSelection;
        }
    }

    /// <summary>
    /// 获取第一个动画窗口
    /// </summary>
    public EditorWindow firstAnimationWindow
    {
        get
        {
            if (m_FirstAnimationWindow == null)
            {
                MethodInfo getAllAnimationWindowsInfo = animationWindowType.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.Static);
                IList animationWindows = getAllAnimationWindowsInfo.Invoke(null, null) as IList;
                if (animationWindows.Count > 0)
                {
                    m_FirstAnimationWindow = animationWindows[0] as EditorWindow;
                }
            }
            return m_FirstAnimationWindow;
        }
    }

    private object animEditor
    {
        get
        {
            if (m_AnimEditor == null)
            {
                FieldInfo animEditorInfo = animationWindowType.GetField("m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic);
                if (firstAnimationWindow)
                {
                    m_AnimEditor = animEditorInfo.GetValue(firstAnimationWindow);
                }
            }
            return m_AnimEditor;
        }
    }

    private object animationWindowState
    {
        get
        {
            if (m_AnimationWindowState == null)
            {
                FieldInfo animationWindowStateInfo = animEditorType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
                if (animEditor != null)
                {
                    m_AnimationWindowState = animationWindowStateInfo.GetValue(animEditor);
                }
            }
            return m_AnimationWindowState;
        }
    }

    private object animationWindowSelection
    {
        get
        {
            if (m_AnimationWindowSelection == null)
            {
                PropertyInfo selectionInfo = animationWindowStateType.GetProperty("selection", BindingFlags.Instance | BindingFlags.Public);
                if (animationWindowState != null)
                {
                    m_AnimationWindowSelection = selectionInfo.GetValue(animationWindowState, null);
                }
            }
            return m_AnimationWindowSelection;
        }
    }

    private PropertyInfo recordingInfo
    {
        get
        {
            if (m_recordingInfo == null)
            {
                m_recordingInfo = animationWindowStateType.GetProperty("recording", BindingFlags.Instance | BindingFlags.Public);
            }
            return m_recordingInfo;
        }
    }

    /// <summary>
    /// 是否正在记录动画
    /// </summary>
    public bool recording
    {
        get { return (bool)recordingInfo.GetValue(animationWindowState, null); }
        set { recordingInfo.SetValue(animationWindowState, value, null); }
    }

    private PropertyInfo currentTimeInfo
    {
        get
        {
            if (m_currentTimeInfo == null)
            {
                m_currentTimeInfo = animationWindowStateType.GetProperty("currentTime", BindingFlags.Instance | BindingFlags.Public);
            }
            return m_currentTimeInfo;
        }
    }

    /// <summary>
    /// 当前记录的时间
    /// </summary>
    public float currentTime
    {
        get
        {
            if (m_CurrentTimeGetFunc == null)
            {
                m_CurrentTimeGetFunc = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), animationWindowState, currentTimeInfo.GetGetMethod());
            }
            return m_CurrentTimeGetFunc();
        }
        set { currentTimeInfo.SetValue(animationWindowState, value, null); }
    }

    private PropertyInfo activeRootGameObjectInfo
    {
        get
        {
            if (m_activeRootGameObjectInfo == null)
            {
                m_activeRootGameObjectInfo = animationWindowStateType.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public);
            }
            return m_activeRootGameObjectInfo;
        }
    }

    /// <summary>
    /// 当前对象的动画根节点对象
    /// </summary>
    public GameObject activeRootGameObject
    {
        get { return (GameObject) activeRootGameObjectInfo.GetValue(animationWindowState, null); }
    }

    private PropertyInfo activeAnimationClipInfo
    {
        get
        {
            if (m_activeAnimationClipInfo == null)
            {
                m_activeAnimationClipInfo = animationWindowStateType.GetProperty("activeAnimationClip", BindingFlags.Instance | BindingFlags.Public);
            }
            return m_activeAnimationClipInfo;
        }
    }

    /// <summary>
    /// 当前活动的动画片段
    /// </summary>
    public AnimationClip activeAnimationClip
    {
        get { return (AnimationClip) activeAnimationClipInfo.GetValue(animationWindowState, null); }
        set { activeAnimationClipInfo.SetValue(animationWindowState, value, null);}
    }

    private FieldInfo onClipSelectionChangedInfo
    {
        get
        {
            if (m_onClipSelectionChangedInfo == null)
            {
#if UNITY_5_4_OR_NEWER
                m_onClipSelectionChangedInfo = animationWindowSelectionType.GetField("onSelectionChanged", BindingFlags.Instance | BindingFlags.Public);
#else
                m_onClipSelectionChangedInfo = animationWindowStateType.GetField("onClipSelectionChanged", BindingFlags.Instance | BindingFlags.Public);
#endif
            }
            return m_onClipSelectionChangedInfo;
        }
    }

    /// <summary>
    /// 动画片段切换事件
    /// </summary>
    public Action onClipSelectionChanged
    {
#if UNITY_5_4_OR_NEWER
        get { return (Action)onClipSelectionChangedInfo.GetValue(animationWindowSelection); }
        set { onClipSelectionChangedInfo.SetValue(animationWindowSelection, value); }
#else
        get { return (Action) onClipSelectionChangedInfo.GetValue(animationWindowState); }
        set { onClipSelectionChangedInfo.SetValue(animationWindowState, value);}
#endif
    }
}
