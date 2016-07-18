using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;

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
        tangentMode = new[] {keyframeX.tangentMode, keyframeY.tangentMode, keyframeZ.tangentMode};
    }

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

    public static List<AnimationPathPoint> MakePoints(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
    {
        List<AnimationPathPoint> points = new List<AnimationPathPoint>();
        for (int i = 0; i < curveX.length; i++)
        {
            Keyframe key = curveX.keys[i];
            AnimationPathPoint pathPoint = new AnimationPathPoint(key,
                GetKeyframeAtTime(curveY, key.time),
                GetKeyframeAtTime(curveZ, key.time)
                );
            points.Add(pathPoint);
        }
        return points;
    }

    private static Keyframe GetKeyframeAtTime(AnimationCurve curve, float time)
    {
        for (int j = 0; j < curve.length; j++)
        {
            if (Mathf.Approximately(curve.keys[j].time, time))
            {
                return curve.keys[j];
            }
        }
        return new Keyframe(time, curve.Evaluate(time));
    }

    public static void ModifyPointTangent(AnimationPathPoint pathPoint, AnimationPathPoint nextPathPoint,
        Vector3 offset, bool isInTangent,
        AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
    {
        Vector3 startTangent;
        Vector3 endTangent;
        CalcTangents(pathPoint, nextPathPoint, out startTangent, out endTangent);

        Keyframe keyframeX = new Keyframe();
        Keyframe keyframeY = keyframeX;
        Keyframe keyframeZ = keyframeX;
        float dx = nextPathPoint.time - pathPoint.time;
        if (isInTangent)
        {
            endTangent += offset;
            Vector3 inTangent = (nextPathPoint.position - endTangent) / dx * 3f;

            keyframeX = new Keyframe(nextPathPoint.time, nextPathPoint.position.x, inTangent.x, nextPathPoint.outTangent.x);
            keyframeY = new Keyframe(nextPathPoint.time, nextPathPoint.position.y, inTangent.y, nextPathPoint.outTangent.y);
            keyframeZ = new Keyframe(nextPathPoint.time, nextPathPoint.position.z, inTangent.z, nextPathPoint.outTangent.z);

            keyframeX.tangentMode = nextPathPoint.tangentMode[0];
            keyframeY.tangentMode = nextPathPoint.tangentMode[1];
            keyframeZ.tangentMode = nextPathPoint.tangentMode[2];
            ModifyPointTangentMode(ref keyframeX, 0);
            ModifyPointTangentMode(ref keyframeY, 0);
            ModifyPointTangentMode(ref keyframeZ, 0);
        }
        else
        {
            startTangent += offset;
            Vector3 outTangent = (startTangent - pathPoint.position) / dx * 3f;

            keyframeX = new Keyframe(pathPoint.time, pathPoint.position.x, pathPoint.inTangent.x, outTangent.x);
            keyframeY = new Keyframe(pathPoint.time, pathPoint.position.y, pathPoint.inTangent.y, outTangent.y);
            keyframeZ = new Keyframe(pathPoint.time, pathPoint.position.z, pathPoint.inTangent.z, outTangent.z);

            keyframeX.tangentMode = pathPoint.tangentMode[0];
            keyframeY.tangentMode = pathPoint.tangentMode[1];
            keyframeZ.tangentMode = pathPoint.tangentMode[2];
            ModifyPointTangentMode(ref keyframeX, 1);
            ModifyPointTangentMode(ref keyframeY, 1);
            ModifyPointTangentMode(ref keyframeZ, 1);
        }

        for (int j = 0; j < curveX.length; j++)
        {
            if (Mathf.Approximately(curveX.keys[j].time, keyframeX.time))
            {
                curveX.MoveKey(j, keyframeX);
                break;
            }
        }
        for (int j = 0; j < curveY.length; j++)
        {
            if (Mathf.Approximately(curveY.keys[j].time, keyframeY.time))
            {
                curveY.MoveKey(j, keyframeY);
                break;
            }
        }
        for (int j = 0; j < curveZ.length; j++)
        {
            if (Mathf.Approximately(curveZ.keys[j].time, keyframeZ.time))
            {
                curveZ.MoveKey(j, keyframeZ);
                break;
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