using UnityEngine;
using System.Collections.Generic;

public class AnimationPathPoint
{
    public float time;
    public Vector3 position;
    public Vector3 inTangent;
    public Vector3 outTangent;
    public int[] tangentMode;

    public Vector3 worldPosition { get; set; }
    public Vector3 worldInTangent { get; set; }
    public Vector3 worldOutTangent { get; set; }

    public AnimationPathPoint(Keyframe keyframeX, Keyframe keyframeY, Keyframe keyframeZ)
    {
        time = keyframeX.time;
        position = new Vector3(keyframeX.value, keyframeY.value, keyframeZ.value);
        inTangent = new Vector3(keyframeX.inTangent, keyframeY.inTangent, keyframeZ.inTangent);
        outTangent = new Vector3(keyframeX.outTangent, keyframeY.outTangent, keyframeZ.outTangent);
#pragma warning disable CS0618 // 类型或成员已过时

#pragma warning disable CS0618 // 类型或成员已过时
        tangentMode = new[] {keyframeX.tangentMode, keyframeY.tangentMode, keyframeZ.tangentMode};
#pragma warning restore CS0618 // 类型或成员已过时

#pragma warning disable CS0618 // 类型或成员已过时
#pragma warning restore CS0618 // 类型或成员已过时
    }
#pragma warning restore CS0618 // 类型或成员已过时

    public static void CalcTangents(AnimationPathPoint pathPoint, AnimationPathPoint nextPathPoint,
        out Vector3 startTangent, out Vector3 endTangent)
    {
        startTangent = pathPoint.position;
        endTangent = nextPathPoint.position;

        float dx = nextPathPoint.time - pathPoint.time;

        startTangent.x += (dx * pathPoint.outTangent.x * 1 / 3);
        startTangent.y += (dx * pathPoint.outTangent.y * 1 / 3);
        startTangent.z += (dx * pathPoint.outTangent.z * 1 / 3);

        endTangent.x -= (dx * nextPathPoint.inTangent.x * 1 / 3);
        endTangent.y -= (dx * nextPathPoint.inTangent.y * 1 / 3);
        endTangent.z -= (dx * nextPathPoint.inTangent.z * 1 / 3);
    }

    public static List<AnimationPathPoint> MakePoints(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, Vector3 initPosition)
    {
        List<AnimationPathPoint> points = new List<AnimationPathPoint>();
        List<float> times = new List<float>();
        if (curveX != null)
        {
            for (int i = 0; i < curveX.length; i++)
            {
                if (!times.Contains(curveX.keys[i].time))
                {
                    times.Add(curveX.keys[i].time);
                }
            }
        }

        if (curveY != null)
        {
            for (int i = 0; i < curveY.length; i++)
            {
                if (!times.Contains(curveY.keys[i].time))
                {
                    times.Add(curveY.keys[i].time);
                }
            }
        }

        if (curveZ != null)
        {
            for (int i = 0; i < curveZ.length; i++)
            {
                if (!times.Contains(curveZ.keys[i].time))
                {
                    times.Add(curveZ.keys[i].time);
                }
            }
        }
        
        times.Sort();

        for (int i = 0; i < times.Count; i++)
        {
            float time = times[i];
            AnimationPathPoint pathPoint = new AnimationPathPoint(
                GetKeyframeAtTime(curveX, time, initPosition.x),
                GetKeyframeAtTime(curveY, time, initPosition.y),
                GetKeyframeAtTime(curveZ, time, initPosition.z)
                );
            points.Add(pathPoint);
        }

        return points;
    }

    private static Keyframe GetKeyframeAtTime(AnimationCurve curve, float time, float initVal)
    {
        if (curve == null)
        {
            return new Keyframe(time, initVal);
        }

        for (int j = 0; j < curve.length; j++)
        {
            if (Mathf.Approximately(curve.keys[j].time, time))
            {
                return curve.keys[j];
            }
        }

        float num = 0.0001f;
        float num2 = (curve.Evaluate(time + num) - curve.Evaluate(time - num)) / (num * 2f);
        return new Keyframe(time, curve.Evaluate(time), num2, num2);
    }

    public static void ModifyPointTangent(AnimationPathPoint pathPoint, AnimationPathPoint nextPathPoint,
        Vector3 offset, bool isInTangent,
        AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
    {
        Vector3 startTangent;
        Vector3 endTangent;
        CalcTangents(pathPoint, nextPathPoint, out startTangent, out endTangent);

        float time;
        Vector3 position;
        Vector3 inTangent;
        Vector3 outTangent;
        int[] tangentMode;

        float dx = nextPathPoint.time - pathPoint.time;
        if (isInTangent)
        {
            time = nextPathPoint.time;
            position = nextPathPoint.position;

            endTangent += offset;
            inTangent = (nextPathPoint.position - endTangent) / dx * 3f;
            outTangent = nextPathPoint.outTangent;
            tangentMode = nextPathPoint.tangentMode;
        }
        else
        {
            time = pathPoint.time;
            position = pathPoint.position;

            startTangent += offset;
            inTangent = pathPoint.inTangent;
            outTangent = (startTangent - pathPoint.position) / dx * 3f;
            tangentMode = pathPoint.tangentMode;
        }

        int leftRight = isInTangent ? 0 : 1;
        ModifyCurveAtKeyframe(curveX, time, position.x, inTangent.x, outTangent.x, tangentMode[0], leftRight);
        ModifyCurveAtKeyframe(curveY, time, position.y, inTangent.y, outTangent.y, tangentMode[1], leftRight);
        ModifyCurveAtKeyframe(curveZ, time, position.z, inTangent.z, outTangent.z, tangentMode[2], leftRight);
    }

    private static void ModifyCurveAtKeyframe(AnimationCurve curve, float time, float value, float inTangent, float outTangent,
        int tangentMode, int leftRight)
    {
        if (curve == null)
        {
            return;
        }

        Keyframe keyframe = new Keyframe(time, value, inTangent, outTangent);
#pragma warning disable CS0618 // 类型或成员已过时
        keyframe.tangentMode = tangentMode;
#pragma warning restore CS0618 // 类型或成员已过时
        ModifyPointTangentMode(ref keyframe, leftRight);

        for (int j = 0; j < curve.length; j++)
        {
            if (Mathf.Approximately(curve.keys[j].time, keyframe.time))
            {
                curve.MoveKey(j, keyframe);
                return;
            }
        }
    }

    private static void ModifyPointTangentMode(ref Keyframe key, int leftRight)
    {
        if (leftRight == 0)
        {
            CurveUtil.SetKeyTangentMode(ref key, 0, CurveUtil.TangentMode.Editable);
            if (!CurveUtil.GetKeyBroken(key))
            {
                key.outTangent = key.inTangent;
                CurveUtil.SetKeyTangentMode(ref key, 1, CurveUtil.TangentMode.Editable);
            }
        }
        else
        {
            CurveUtil.SetKeyTangentMode(ref key, 1, CurveUtil.TangentMode.Editable);
            if (!CurveUtil.GetKeyBroken(key))
            {
                key.inTangent = key.outTangent;
                CurveUtil.SetKeyTangentMode(ref key, 0, CurveUtil.TangentMode.Editable);
            }
        }
    }
}