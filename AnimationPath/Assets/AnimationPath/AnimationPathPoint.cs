using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationPathPoint
{
    public float time;
    public Vector3 position;
    public Vector3 inTangent;
    public Vector3 outTangent;
    public Vector3 worldPosition;

    public AnimationPathPoint(Keyframe keyframeX, Keyframe keyframeY, Keyframe keyframeZ)
    {
        time = keyframeX.time;
        position = new Vector3(keyframeX.value, keyframeY.value, keyframeZ.value);
        inTangent = new Vector3(keyframeX.inTangent, keyframeY.inTangent, keyframeZ.inTangent);
        outTangent = new Vector3(keyframeX.outTangent, keyframeY.outTangent, keyframeZ.outTangent);
        worldPosition = position;
    }

    public static void CalcTangents(AnimationPathPoint pathPoint, AnimationPathPoint nextPathPoint,
        out Vector3 startTangent, out Vector3 endTangent)
    {
        startTangent = pathPoint.position;
        endTangent = nextPathPoint.position;

        float dx = nextPathPoint.time - pathPoint.time;

        startTangent.x += dx * pathPoint.outTangent.x * 1 / 3;
        startTangent.y += dx * pathPoint.outTangent.y * 1 / 3;
        startTangent.z += dx * pathPoint.outTangent.z * 1 / 3;

        endTangent.x -= dx * nextPathPoint.inTangent.x * 1 / 3;
        endTangent.y -= dx * nextPathPoint.inTangent.y * 1 / 3;
        endTangent.z -= dx * nextPathPoint.inTangent.z * 1 / 3;
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
            if (curve.keys[j].time == time)
            {
                return curve.keys[j];
            }
        }
        return new Keyframe(time, curve.Evaluate(time));
    }
}