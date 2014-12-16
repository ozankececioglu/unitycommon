using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;

[System.Serializable]
public struct Sphere
{
    public Vector3 center;
    public float radius;

    public Sphere(Vector3 acenter, float aradius)
    {
        center = acenter;
        radius = aradius;
    }

    public void Draw()
    {
        Draw(Color.white);
    }
    public void Draw(Color color)
    {
        Common.DrawStar(center, radius);
    }
}

// Rotated bounding box
[System.Serializable]
public struct RotatableBounds
{
    public Vector3 extents;
    public Vector3 center;
    Vector3 maxisX;
    Vector3 maxisY;
    Vector3 maxisZ;
    public Vector3 axisX { get { return maxisX; } }
    public Vector3 axisY { get { return maxisY; } }
    public Vector3 axisZ { get { return maxisZ; } }

    public RotatableBounds(Bounds tlocal, Transform ttransform)
    {
        var matrix = ttransform.localToWorldMatrix;
        center = matrix * tlocal.center.ToVector4();
        maxisX = matrix * Vector3.left;
        maxisY = matrix * Vector3.up;
        maxisZ = matrix * Vector3.forward;
        extents = new Vector3(maxisX.magnitude, maxisY.magnitude, maxisZ.magnitude);
        maxisX /= extents.x;
        maxisY /= extents.y;
        maxisZ /= extents.z;
        extents.Scale(tlocal.extents);
    }
    public RotatableBounds(Bounds tlocal, Matrix4x4 matrix)
    {
        center = matrix * tlocal.center.ToVector4();
        maxisX = matrix * Vector3.left;
        maxisY = matrix * Vector3.up;
        maxisZ = matrix * Vector3.forward;
        extents = new Vector3(maxisX.magnitude, maxisY.magnitude, maxisZ.magnitude);
        maxisX /= extents.x;
        maxisY /= extents.y;
        maxisZ /= extents.z;
        extents.Scale(tlocal.extents);
    }

    public IEnumerable<Vector3> Corners()
    {
        Vector3 taxisX = maxisX * extents.x;
        Vector3 taxisY = maxisY * extents.y;
        Vector3 taxisZ = maxisZ * extents.z;

        yield return center + taxisX + taxisY + taxisZ;
        yield return center + taxisX + taxisY - taxisZ;
        yield return center + taxisX - taxisY + taxisZ;
        yield return center + taxisX - taxisY - taxisZ;
        yield return center - taxisX + taxisY + taxisZ;
        yield return center - taxisX + taxisY - taxisZ;
        yield return center - taxisX - taxisY + taxisZ;
        yield return center - taxisX - taxisY - taxisZ;
    }
    public IEnumerable<Plane> Planes()
    {
        Vector3 taxisX = maxisX * extents.x;
        Vector3 taxisY = maxisY * extents.y;
        Vector3 taxisZ = maxisZ * extents.z;

        yield return new Plane(-maxisX, center + taxisX);
        yield return new Plane(maxisX, center - taxisX);
        yield return new Plane(-maxisY, center + taxisY);
        yield return new Plane(maxisY, center - taxisY);
        yield return new Plane(-maxisZ, center + taxisZ);
        yield return new Plane(maxisZ, center - taxisZ);
    }
    public Bounds ToBounds()
    {
        var enumer = Corners().GetEnumerator();
        enumer.MoveNext();
        var pbounds = new Bounds(enumer.Current, new Vector3());
        while (enumer.MoveNext()) {
            pbounds.Encapsulate(enumer.Current);
        }
        return pbounds;
    }

    public void Rotate(Quaternion quat)
    {
        center = quat * center;
        maxisX = quat * maxisX;
        maxisY = quat * maxisY;
        maxisZ = quat * maxisZ;
    }
    public void Translate(Vector3 position)
    {
        center = maxisX * position.x + maxisY * position.y + maxisZ * position.z; 
    }

    public Vector3 WorldToLocal(Vector3 point)
    {
        Vector3 plocal = point - center;
        return new Vector3(Vector3.Dot(plocal, maxisX), Vector3.Dot(plocal, maxisY), Vector3.Dot(plocal, maxisZ));
    }
    public Vector3 LocalToWorld(Vector3 point)
    {
        return center + maxisX * point.x + maxisY * point.y + maxisZ * point.z;
    }
    public Vector3 WorldDirectionToLocal(Vector3 direction)
    {
        return new Vector3(Vector3.Dot(direction, maxisX), Vector3.Dot(direction, maxisY), Vector3.Dot(direction, maxisZ));
    }
    public Vector3 LocalDirectionToWorld(Vector3 direction)
    {
        return maxisX * direction.x + maxisY * direction.y + maxisZ * direction.z;
    }

    public bool Contains(Vector3 point)
    {
        Vector3 projected = WorldToLocal(point);
        return Mathf.Abs(projected.x) <= extents.x
                && Mathf.Abs(projected.y) <= extents.y
                && Mathf.Abs(projected.z) <= extents.z;
    }
    public bool Contains(Bounds bounds)
    {
        foreach (var corner in bounds.Corners()) {
            if (!Contains(corner)) {
                return false;
            }
        }
        return true;
    }
    public bool Contains(Sphere sphere)
    {
        var center = WorldToLocal(sphere.center);
        var textents = extents - Vector3.one * sphere.radius;
        return Mathf.Abs(center.x) < textents.x && Mathf.Abs(center.y) < textents.y && Mathf.Abs(center.z) < textents.z;
    }
    public void Encapsulate(Vector3 point)
    {
        Vector3 projected = WorldToLocal(point);
        if (projected.x > extents.x) {
            center.x = (projected.x - extents.x) * 0.5f;
            extents.x = (extents.x + projected.x) * 0.5f;
        } else if (projected.x < -extents.x) {
            center.x = (projected.x + extents.x) * 0.5f;
            extents.x = (extents.x - projected.x) * 0.5f;
        }

        if (projected.y > extents.y) {
            center.y = (projected.y - extents.y) * 0.5f;
            extents.y = (extents.y + projected.y) * 0.5f;
        } else if (projected.y < -extents.y) {
            center.y = (projected.y + extents.y) * 0.5f;
            extents.y = (extents.y - projected.y) * 0.5f;
        }

        if (projected.z > extents.z) {
            center.z = (projected.z - extents.z) * 0.5f;
            extents.z = (extents.z + projected.z) * 0.5f;
        } else if (projected.z < -extents.z) {
            center.z = (projected.z + extents.z) * 0.5f;
            extents.z = (extents.z - projected.z) * 0.5f;
        }
    }
    public void Encapsulate(Bounds bounds)
    {
        foreach (var corner in bounds.Corners()) {
            Encapsulate(corner);
        }
    }
    public void Encapsulate(RotatableBounds bounds)
    {
        foreach (var corner in bounds.Corners()) {
            Encapsulate(corner);
        }
    }

    public Vector3 KeepInside(Sphere sphere)
    {
        var center = WorldToLocal(sphere.center);
        var radius = sphere.radius;
        Vector3 result = center;

        if (center.x + radius > this.extents.x) {
            result.x = this.extents.x - radius;
        } else if (center.x - radius < -this.extents.x) {
            result.x = -this.extents.x + radius;
        }

        if (center.y + radius > this.extents.y) {
            result.y = this.extents.y - radius;
        } else if (center.y - radius < -this.extents.y) {
            result.y = -this.extents.y + radius;
        }

        if (center.z + radius > this.extents.z) {
            result.z = this.extents.z - radius;
        } else if (center.z - radius < -this.extents.z) {
            result.z = -this.extents.z + radius;
        }

        return LocalDirectionToWorld(result - center);
    }

    public bool IntersectsLine(Vector3 tstart, Vector3 tend)
    {
        float m, n;
        return IntersectsLine(tstart, tend, out m, out n);
    }
    public bool IntersectsLine(Vector3 tstart, Vector3 tend, out float m, out float n)
    {
        Vector3 start = WorldToLocal(tstart);
        Vector3 end = WorldToLocal(tend);
        Vector3 dir = end - start;
        m = float.MinValue;
        n = float.MaxValue;

        if (!dir.x.IsZero()) {
            float extent = extents.x;
            m = (extent - start.x) / dir.x;
            n = (-extent - start.x) / dir.x;
            if (m > n) {
                float temp = m;
                m = n;
                n = temp;
            }
        }

        if (!dir.y.IsZero()) {
            float extent = extents.y;
            float tm = (extent - start.y) / dir.y;
            float tn = (-extent - start.y) / dir.y;
            if (tm > tn) {
                float temp = tm;
                tm = tn;
                tn = temp;
            }

            if (tm > m)
                m = tm;
            if (tn < n)
                n = tn;
        }

        if (!dir.z.IsZero()) {
            float extent = extents.z;
            float tm = (extent - start.z) / dir.z;
            float tn = (-extent - start.z) / dir.z;
            if (tm > tn) {
                float temp = tm;
                tm = tn;
                tn = temp;
            }

            if (tm > m)
                m = tm;
            if (tn < n)
                n = tn;
        }

        if (m < 0f && n < 0f || m > 1.0f && n > 1.0f)
            return false;

        Vector3 middle = start + dir * (m + n) * 0.5f;
        return Mathf.Abs(middle.x) <= extents.x && Mathf.Abs(middle.y) <= extents.y && Mathf.Abs(middle.z) <= extents.z;
    }
    public bool IntersectsPolyline(IEnumerable<Vector3> polyline)
    {
        IEnumerator<Vector3> enumerator = polyline.GetEnumerator();
        enumerator.MoveNext();
        Vector3 odd = enumerator.Current;
        Vector3 even = new Vector3();
        bool isOdd = false;
        while (enumerator.MoveNext()) {
            if (isOdd) {
                odd = enumerator.Current;
            } else {
                even = enumerator.Current;
            }

            float m, n;
            if (IntersectsLine(odd, even, out m, out n)) {
                return true;
            }

            isOdd = !isOdd;
        }

        return false;
    }

    public void Draw()
    {
        Draw(Color.white);
    }
    public void Draw(Color color)
    {
        Vector3 x = maxisX * extents.x;
        Vector3 y = maxisY * extents.y;
        Vector3 z = maxisZ * extents.z;

        Debug.DrawLine(center + x + y + z, center + x + y - z, color);
        Debug.DrawLine(center + x + y + z, center + x - y + z, color);
        Debug.DrawLine(center + x + y + z, center - x + y + z, color);
        Debug.DrawLine(center + x - y - z, center - x - y - z, color);
        Debug.DrawLine(center + x - y - z, center + x + y - z, color);
        Debug.DrawLine(center + x - y - z, center + x - y + z, color);
        Debug.DrawLine(center - x + y - z, center + x + y - z, color);
        Debug.DrawLine(center - x + y - z, center - x - y - z, color);
        Debug.DrawLine(center - x + y - z, center - x + y + z, color);
        Debug.DrawLine(center - x - y + z, center + x - y + z, color);
        Debug.DrawLine(center - x - y + z, center - x + y + z, color);
        Debug.DrawLine(center - x - y + z, center - x - y - z, color);
    }
}

public class CoupleEnumerator<T>
{
    IEnumerator<T> enumer;
    T previous;
    int index;

    public CoupleEnumerator(IEnumerable<T> enumable)
    {
        enumer = enumable.GetEnumerator();
        index = enumer.MoveNext() ? 0 : -1;
        previous = default(T);
    }

    public virtual bool MoveNext()
    {
        if (Valid) {
            previous = enumer.Current;
            index = enumer.MoveNext() ? index + 1 : -1;
        }
        return Valid;
    }
    public virtual void Reset()
    {
        enumer.Reset();
        index = enumer.MoveNext() ? 0 : -1;
    }

    public int Index { get { return index; } }
    public bool Valid { get { return index >= 0; } }
    public T Previous { get { return previous; } }
    public T Current { get { return enumer.Current; } }
}

public class Geometry
{
    public static Vector3 NearestPointOfLine(Vector3 start, Vector3 end, Vector3 point, out float projection, out float lineLength)
    {
        Vector3 dir = end - start;
        lineLength = dir.magnitude;
        dir /= lineLength;
        Vector3 local = point - start;
        projection = Vector3.Dot(local, dir);

        if (projection < 0f) {
            return start;
        } else if (projection > lineLength) {
            return end;
        } else {
            return start + dir * projection;
        }
    }
    public static Vector3 NearestPointOfLine(Vector3 start, Vector3 end, Vector3 point, out float projection)
    {
        float lineLength;
        return NearestPointOfLine(start, end, point, out projection, out lineLength);
    }
    public static Vector3 NearestPointOfLine(Vector3 start, Vector3 end, Vector3 point)
    {
        float lineLength, projection;
        return NearestPointOfLine(start, end, point, out projection, out lineLength);
    }

    public static float LinePointDistance(Vector3 start, Vector3 end, Vector3 point, out float projection, out float lineLength)
    {
        Vector3 linePoint = NearestPointOfLine(start, end, point, out projection, out lineLength);
        return (point - linePoint).magnitude;
    }
    public static float LinePointDistance(Vector3 start, Vector3 end, Vector3 point, out float projection)
    {
        float lineLength;
        Vector3 linePoint = NearestPointOfLine(start, end, point, out projection, out lineLength);
        return (point - linePoint).magnitude;
    }
    public static float LinePointDistance(Vector3 start, Vector3 end, Vector3 point)
    {
        float projection, lineLength;
        Vector3 linePoint = NearestPointOfLine(start, end, point, out projection, out lineLength);
        return (point - linePoint).magnitude;
    }

    public static Vector3 NearestPointOfLineStrip(IEnumerable<Vector3> lines, Vector3 point, out float projection, out float distance, out bool right, out bool outOfLine)
    {
        projection = float.NaN;
        distance = float.MaxValue;
        right = true;
        var totalLength = 0f;
        var up = Vector3.up;
        var enumer = new CoupleEnumerator<Vector3>(lines);
        var result = new Vector3();
        var tsegmentProjection = float.NaN;
        while (enumer.MoveNext()) {
            float lineLength;
            var linePoint = NearestPointOfLine(enumer.Previous, enumer.Current, point, out tsegmentProjection, out lineLength);
            var tdistance = (linePoint - point).magnitude;
            if (tdistance < distance) {
                distance = tdistance;
                projection = totalLength + Mathf.Clamp(tsegmentProjection, 0f, lineLength);
                right = Vector3.Dot(Vector3.Cross(enumer.Current - enumer.Previous, point - enumer.Previous), up) > 0f;
                result = linePoint;
            }
            totalLength += lineLength;
        }
        outOfLine = projection.IsZero() || (projection - totalLength).IsZero();

        return result;
    }
    public static Vector3 NearestPointOfLineStrip(IEnumerable<Vector3> lines, Vector3 point, out float projection, out float distance, out bool right)
    {
        bool outOfLine;
        return NearestPointOfLineStrip(lines, point, out projection, out distance, out right, out outOfLine);
    }
    public static Vector3 NearestPointOfLineStrip(IEnumerable<Vector3> lines, Vector3 point, out float projection)
    {
        float distance;
        bool right, outOfLine;
        return NearestPointOfLineStrip(lines, point, out projection, out distance, out right, out outOfLine);
    }
    public static Vector3 NearestPointOfLineStrip(IEnumerable<Vector3> lines, Vector3 point)
    {
        float distance, projection;
        bool right, outOfLine;
        return NearestPointOfLineStrip(lines, point, out projection, out distance, out right, out outOfLine);
    }

    public static float LineLineDistance(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
    {
        float distance1, distance2;
        return LineLineDistance(start1, end1, start2, end2, out distance1, out distance2);
    }
    public static float LineLineDistance(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out float distance1, out float distance2)
    {
        Vector3 u = end1 - start1;
        Vector3 v = end2 - start2;
        Vector3 w = start1 - start2;
        float a = Vector3.Dot(u, u);         // always >= 0
        float b = Vector3.Dot(u, v);
        float c = Vector3.Dot(v, v);         // always >= 0
        float d = Vector3.Dot(u, w);
        float e = Vector3.Dot(v, w);
        float D = a * c - b * b;        // always >= 0
        float sN, sD = D;       // sc = sN / sD, default sD = D >= 0
        float tN, tD = D;       // tc = tN / tD, default tD = D >= 0

        // compute the line parameters of the two closest points
        if (Mathf.Approximately(0.0f, D)) { // the lines are almost parallel
            sN = 0.0f;         // force using point P0 on segment S1
            sD = 1.0f;         // to prevent possible division by 0.0 later
            tN = e;
            tD = c;
        } else {                 // get the closest points on the infinite lines
            sN = (b * e - c * d);
            tN = (a * e - b * d);
            if (sN < 0.0f) {        // sc < 0 => the s=0 edge is visible
                sN = 0.0f;
                tN = e;
                tD = c;
            } else if (sN > sD) {  // sc > 1  => the s=1 edge is visible
                sN = sD;
                tN = e + b;
                tD = c;
            }
        }

        if (tN < 0.0f) {            // tc < 0 => the t=0 edge is visible
            tN = 0.0f;
            // recompute sc for this edge
            if (-d < 0.0f)
                sN = 0.0f;
            else if (-d > a)
                sN = sD;
            else {
                sN = -d;
                sD = a;
            }
        } else if (tN > tD) {      // tc > 1  => the t=1 edge is visible
            tN = tD;
            // recompute sc for this edge
            if ((-d + b) < 0.0f)
                sN = 0;
            else if ((-d + b) > a)
                sN = sD;
            else {
                sN = (-d + b);
                sD = a;
            }
        }

        // finally do the division to get sc and tc
        distance1 = (Mathf.Approximately(0.0f, sN) ? 0.0f : sN / sD);
        distance2 = (Mathf.Approximately(0.0f, tN) ? 0.0f : tN / tD);

        // get the difference of the two closest points
        Vector3 dP = w + (distance1 * u) - (distance2 * v);  // =  S1(sc) - S2(tc)

        return dP.magnitude;   // return the closest distance
    }

    public static Rect RectScale(Rect target, float width, float height)
    {
        Rect result = new Rect();
        result.x = target.x / width;
        result.y = target.y / height;
        result.width = target.width / width;
        result.height = target.height / height;
        return result;
    }
    public static Rect RectScaleInverse(Rect original, Rect scale)
    {
        Rect result = new Rect();
        result.x = original.x + original.width * scale.x;
        result.y = original.y + original.height * scale.y;
        result.width = original.width * scale.width;
        result.height = original.height * scale.height;
        return result;
    }

    public static float AreaOf(Bounds bounds)
    {
        return bounds.size.x * bounds.size.z;
    }
    public static float VolumeOf(Bounds bounds)
    {
        return bounds.size.x * bounds.size.y * bounds.size.z;
    }

    public static float Angle(Vector2 first, Vector2 second)
    {
        // Since Vector2.Angle always returns positive...
        return Vector2.Angle(first, second) * Mathf.Sign(first.x * second.y - first.y * second.x);
    }
    public static float ClampAngle(float angle)
    {
        angle = angle % 360;
        if (angle > 180f) {
            angle -= 360f;
        } else if (angle < -180f) {
            angle += 360f;
        }
        return angle;
    }

    public static Vector3 Min(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
    }
    public static Vector3 Min(params Vector3[] vectors)
    {
        Vector3 result = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        foreach (var vector in vectors) {
            if (result.x > vector.x)
                result.x = vector.x;
            if (result.y > vector.y)
                result.y = vector.y;
            if (result.z > vector.z)
                result.z = vector.z;
        }
        return result;
    }
    public static Vector3 Max(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
    }
    public static Vector3 Max(params Vector3[] vectors)
    {
        Vector3 result = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var vector in vectors) {
            if (result.x < vector.x)
                result.x = vector.x;
            if (result.y < vector.y)
                result.y = vector.y;
            if (result.z < vector.z)
                result.z = vector.z;
        }
        return result;
    }
    public static Vector3 Mean(params Vector3[] vectors)
    {
        if (vectors.Length > 0) {
            var total = new Vector3();
            foreach (var vector in vectors) {
                total += vector;
            }
            return total / vectors.Length;
        } else {
            return Vector3.zero;
        }
    }

    public static float CircleIntersectionOnLine(Vector3 center, float radius, Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float length = dir.magnitude;
        dir /= length;
        Vector3 local = center - start;
        float distanceOnLine = Vector3.Dot(local, dir);
        float distanceToLineSqr = (local - (dir * distanceOnLine)).sqrMagnitude;
        float distanceSqr = radius * radius;

        if (distanceSqr > distanceToLineSqr) {
            float solution = Mathf.Sqrt(distanceSqr - distanceToLineSqr);
            return (distanceOnLine + Mathf.Sign(radius) * solution) / length;
        } else {
            return -1f;
        }
    }

    public static bool PositionOfDistanceOnLine(CoupleEnumerator<Vector3> enumer, float distance, out Vector3 result)
    {
        while (enumer.MoveNext()) {
            var dir = enumer.Current - enumer.Previous;
            var length = dir.magnitude;
            if (length > distance) {
                result = enumer.Previous + dir * (distance / length);
                return true;
            }
            distance -= length;
        }
        result = Vector3.zero;
        return false;
    }

    public static bool PositionOfDistanceNearLine(CoupleEnumerator<Vector3> enumer, float projection, float lateralOffset, bool onLeft, out Vector3 result, out float angle)
    {
        float angleDirection = onLeft ? 90f : -90f;
        angle = 0;
        while (enumer.MoveNext()) {
            var dir = enumer.Current - enumer.Previous;
            var length = dir.magnitude;
            if (length > projection) {
                result = enumer.Previous + dir * (projection / length);
                RotateAtYAxis(ref dir, angleDirection);
                result = result + dir * (lateralOffset / length);
                //                float y = enumer.Current.z - enumer.Previous.z;
                //                float x = enumer.Current.x - enumer.Previous.x;
                var rotation = Quaternion.LookRotation(result - enumer.Current);
                rotation *= Quaternion.AngleAxis(90f, Vector3.up);
                angle = rotation.eulerAngles.y;
                //angle = ( float )Math.Atan(( double )y / ( double )x);
                //angle = angle * (float)( 180.0 / Math.PI );
                if (enumer.Current.z < 0)
                    angle += 180f;
                return true;
            }
            projection -= length;
        }
        result = Vector3.zero;
        return false;
    }

    public static void RotateAtYAxis(ref Vector3 v, float angle)
    {
        double sin = Math.Sin(angle * Math.PI / 180.0);
        double cos = Math.Cos(angle * Math.PI / 180.0);
        double tx = v.x;
        double tz = v.z;
        v.x = (float)((cos * tx) + (sin * tz));
        v.z = (float)((cos * tz) - (sin * tx));
    }

    public static bool CircleIntersectionOfLines(CoupleEnumerator<Vector3> enumer, Vector3 point, float distance, out Vector3 result)
    {
        while (enumer.MoveNext()) {
            var distanceOnLine = CircleIntersectionOnLine(point, distance, enumer.Previous, enumer.Current);
            if (distanceOnLine >= 0f && distanceOnLine <= 1f) {
                result = enumer.Previous + (enumer.Current - enumer.Previous) * distanceOnLine;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    public static List<Vector3> TrailOnLine(IEnumerable<Vector3> line, float startDistance, IEnumerable<float> partLengths)
    {
        var result = new List<Vector3>();

        var enumer = new CoupleEnumerator<Vector3>(line);
        enumer.MoveNext();
        if (!enumer.Valid)
            return result;

        Vector3 start;
        Vector3 end;
        if (!PositionOfDistanceOnLine(enumer, startDistance, out start))
            return result;

        result.Add(start);
        foreach (float length in partLengths) {
            if (!CircleIntersectionOfLines(enumer, start, length, out end))
                break;

            result.Add(end);
            start = end;
        }
        return result;
    }

    public static float LengthOfLine(IEnumerable<Vector3> line)
    {
        var enumer = line.GetEnumerator();
        if (!enumer.MoveNext())
            return 0f;

        float result = 0f;
        var start = enumer.Current;
        while (enumer.MoveNext()) {
            var end = enumer.Current;
            result += (end - start).magnitude;
            start = end;
        }
        return result;
    }

    public static Vector3 InterpolateCurve(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4, float u)
    {
        return new Vector3(
                         point1.x * (-0.5f * u * u * u + u * u - 0.5f * u) +
                         point2.x * (1.5f * u * u * u + -2.5f * u * u + 1.0f) +
                         point3.x * (-1.5f * u * u * u + 2.0f * u * u + 0.5f * u) +
                         point4.x * (0.5f * u * u * u - 0.5f * u * u),

                         point1.y * (-0.5f * u * u * u + u * u - 0.5f * u) +
                         point2.y * (1.5f * u * u * u + -2.5f * u * u + 1.0f) +
                         point3.y * (-1.5f * u * u * u + 2.0f * u * u + 0.5f * u) +
                         point4.y * (0.5f * u * u * u - 0.5f * u * u),

                         point1.z * (-0.5f * u * u * u + u * u - 0.5f * u) +
                         point2.z * (1.5f * u * u * u + -2.5f * u * u + 1.0f) +
                         point3.z * (-1.5f * u * u * u + 2.0f * u * u + 0.5f * u) +
                         point4.z * (0.5f * u * u * u - 0.5f * u * u));
    }

    public static List<Vector3> InterpolateCurve(IEnumerable<Vector3> points, float minInterval)
    {
        var result = new List<Vector3>();
        var enumer = points.GetEnumerator();
        if (!enumer.MoveNext()) {
            return result;
        }

        Vector3 first, second, third, fourth;
        first = enumer.Current;
        result.Add(first);
        second = first;
        if (!enumer.MoveNext()) {
            return result;
        }

        third = enumer.Current;
        if (enumer.MoveNext()) {
            fourth = enumer.Current;
            if (Vector3.Distance(second, third) > minInterval) {
                result.Add(InterpolateCurve(first, second, third, fourth, 0.5f));
            }
            while (enumer.MoveNext()) {

            }
        }

        fourth = third;
        if (Vector3.Distance(second, third) > minInterval) {
            result.Add(InterpolateCurve(first, second, third, fourth, 0.5f));
        }
        result.Add(third);

        return result;
    }

    public static void DistanceToLine(Vector3 start, Vector3 end, Vector3 point, out float distanceOnLine, out float distanceToLine)
    {
        Vector3 dir = end - start;
        float length = dir.magnitude;
        dir /= length;
        Vector3 local = point - start;
        distanceOnLine = Vector3.Dot(local, dir);

        if (distanceOnLine < 0f) {
            distanceToLine = local.magnitude;
        } else if (distanceOnLine > length) {
            distanceToLine = (point - end).magnitude;
        } else {
            distanceToLine = (local - (dir * distanceOnLine)).magnitude;
        }
    }
    public static float DistanceToLineGroup(IEnumerable<Vector3> line, Vector3 point, out float projection)
    {
        bool atRight, onLine;
        var result = DistanceToLine(line, point, out atRight, out onLine, out projection);
        if (!atRight) {
            result = -result;
        }
        return result;
    }
    public static float DistanceToLine(IEnumerable<Vector3> line, Vector3 point)
    {
        bool atRight, onLine;
        float projectionOnLine;
        return DistanceToLine(line, point, out atRight, out onLine, out projectionOnLine);
    }
    public static float DistanceToLine(IEnumerable<Vector3> line, Vector3 point, out bool atRight, out bool onLine, out float projectionOnLine)
    {
        float distance = float.MaxValue;
        var enumer = new CoupleEnumerator<Vector3>(line);
        atRight = true;
        onLine = false;
        projectionOnLine = 0f;
        float totalLength = 0f;
        while (enumer.MoveNext()) {
            float tempDistanceToLine = 0;
            float tempDistanceOnLine;
            var diff = enumer.Current - enumer.Previous;
            totalLength += diff.magnitude;
            Geometry.DistanceToLine(enumer.Previous, enumer.Current, point, out tempDistanceOnLine, out tempDistanceToLine);

            if (tempDistanceToLine < distance) {
                projectionOnLine = tempDistanceOnLine + totalLength;
                distance = tempDistanceToLine;

                Vector2 startV = new Vector2(enumer.Previous.x, enumer.Previous.z);
                Vector2 endV = new Vector2(enumer.Current.x, enumer.Current.z);
                Vector2 pointV = new Vector2(point.x, point.z);
                Vector2 diffV = endV - startV;
                Vector2 diffPoint = pointV - startV;
                double dot = diffV.x * diffPoint.y - diffV.y * diffPoint.x;
                //Vector3 tempPoint = new Vector3(-point.z, point.y, point.x);
                //float dot = diff.x * tempPoint.x /*+ diff.y + tempPoint.y */+ diff.z * tempPoint.z;
                if (dot == 0) {
                    onLine = true;
                } else {
                    atRight = dot > 0;
                    onLine = false;
                }
            }
        }
        return distance;
    }
}
