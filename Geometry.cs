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
public class RBounds
{
	Vector3 extents;
	Vector3 center;
	Vector3 axisX;
	Vector3 axisY;
	Vector3 axisZ;

	public RBounds()
	{
		axisX = Vector3.right;
		axisY = Vector3.up;
		axisZ = Vector3.forward;
	}
	public RBounds(Bounds tlocal, Transform ttransform) { Set(tlocal, ttransform); }

	public Vector3 Extents { get { return extents; } set { extents = value; } }

	public void Set(Bounds tlocal, Transform ttransform)
	{
		extents = tlocal.extents;
		center = ttransform.TransformPoint(tlocal.center);
		axisX = ttransform.right;
		axisY = ttransform.up;
		axisZ = ttransform.forward;
	}

	public IEnumerable<Vector3> Corners()
	{
		Vector3 taxisX = axisX * extents.x;
		Vector3 taxisY = axisY * extents.y;
		Vector3 taxisZ = axisZ * extents.z;

		yield return center + taxisX + taxisY + taxisZ;
		yield return center + taxisX + taxisY - taxisZ;
		yield return center + taxisX - taxisY + taxisZ;
		yield return center + taxisX - taxisY - taxisZ;
		yield return center - taxisX + taxisY + taxisZ;
		yield return center - taxisX + taxisY - taxisZ;
		yield return center - taxisX - taxisY + taxisZ;
		yield return center - taxisX - taxisY - taxisZ;
	}
	public Bounds ToBounds()
	{
		Bounds pbounds = new Bounds(center, new Vector3());
		foreach (Vector3 corner in Corners())
			pbounds.Encapsulate(corner);
		return pbounds;
	}

	public Vector3 WorldToLocal(Vector3 point)
	{
		Vector3 plocal = point - center;
		return new Vector3(Vector3.Dot(plocal, axisX), Vector3.Dot(plocal, axisY), Vector3.Dot(plocal, axisZ));
	}
	public Vector3 LocalToWorld(Vector3 point)
	{
		return center + axisX * point.x + axisY * point.y + axisZ * point.z;
	}
	public Vector3 WorldDirectionToLocal(Vector3 direction)
	{
		return new Vector3(Vector3.Dot(direction, axisX), Vector3.Dot(direction, axisY), Vector3.Dot(direction, axisZ));
	}
	public Vector3 LocalDirectionToWorld(Vector3 direction)
	{
		return axisX * direction.x + axisY * direction.y + axisZ * direction.z;
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
		Vector3 x = axisX * extents.x;
		Vector3 y = axisY * extents.y;
		Vector3 z = axisZ * extents.z;

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

[System.Serializable]
public class SerializableQuaternion
{
	public float x, y, z, w;

	public SerializableQuaternion(Quaternion q)
	{
		x = q.x;
		y = q.y;
		z = q.z;
		w = q.w;
	}
	public static implicit operator SerializableQuaternion(Quaternion q)
	{
		return new SerializableQuaternion(q);
	}
	public static implicit operator Quaternion(SerializableQuaternion q)
	{
		return new Quaternion(q.x, q.y, q.z, q.w);
	}
}

[System.Serializable]
public class SerialiazableVector3
{
	public float x, y, z;

	public SerialiazableVector3(Vector3 v)
	{
		x = v.x;
		y = v.y;
		z = v.z;
	}
	public static implicit operator SerialiazableVector3(Vector3 v)
	{
		return new SerialiazableVector3(v);
	}
	public static implicit operator Vector3(SerialiazableVector3 v)
	{
		return new Vector3(v.x, v.y, v.z);
	}
}

public class CoupleEnumerator<T>
{
	IEnumerator<T> enumer;
	T previous;
	bool valid;

	public CoupleEnumerator(IEnumerable<T> enumable)
	{
		enumer = enumable.GetEnumerator();
		Reset();
	}

	public virtual bool MoveNext()
	{
		if (valid) {
			previous = enumer.Current;
			if (!enumer.MoveNext()) {
				valid = false;
			}
		}
		return valid;
	}
	public virtual void Reset()
	{
		enumer.Reset();
		valid = enumer.MoveNext();
	}

	public bool Valid
	{
		get { return valid; }
	}
	public T Previous
	{
		get { return previous; }
	}
	public T Current
	{
		get { return enumer.Current; }
	}
}

public class Geometry
{
	public static void LinePointDistance(Vector3 start, Vector3 end, Vector3 point, out float distanceOnLine, out float distanceToLine)
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
	public static void ClippedLinePointDistance(Vector3 start, Vector3 end, Vector3 point, out float distanceOnLine, out float distanceToLine)
	{
		Vector3 dir = end - start;
		float length = dir.magnitude;
		dir /= length;
		Vector3 local = point - start;
		distanceOnLine = Vector3.Dot(local, dir);

		if (distanceOnLine < 0f) {
			distanceToLine = local.magnitude;
			distanceOnLine = 0f;
		} else if (distanceOnLine > length) {
			distanceToLine = (point - end).magnitude;
			distanceOnLine = length;
		} else {
			distanceToLine = (local - (dir * distanceOnLine)).magnitude;
		}
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
        if ( !enumer.MoveNext() )
        {
            return result;
        }

        Vector3 first, second, third, fourth;
        first = enumer.Current;
        result.Add(first);
        second = first;
        if ( !enumer.MoveNext() )
        {
            return result;
        }

        third = enumer.Current;
        if ( enumer.MoveNext() )
        {
            fourth = enumer.Current;
            if ( Vector3.Distance(second, third) > minInterval )
            {
                result.Add(InterpolateCurve(first, second, third, fourth, 0.5f));
            }
            while ( enumer.MoveNext() )
            {

            }
        }

        fourth = third;
        if ( Vector3.Distance(second, third) > minInterval )
        {
            result.Add(InterpolateCurve(first, second, third, fourth, 0.5f));
        }
        result.Add(third);

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
        float distance = 100000f;
        var enumer = new CoupleEnumerator<Vector3>(line);
        atRight = true;
        onLine = false;
        projectionOnLine = 0;
        float totalLength = 0;
        while ( enumer.MoveNext() )
        {
            float tempDistanceToLine = 0;
            float tempDistanceOnLine;
            var diff = enumer.Current - enumer.Previous;
            totalLength += diff .magnitude;
            Geometry.LinePointDistance(enumer.Previous, enumer.Current, point, out tempDistanceOnLine, out tempDistanceToLine);
            
            if ( tempDistanceToLine < distance )
            {
                projectionOnLine = tempDistanceOnLine + totalLength;
                distance = tempDistanceToLine;
                Vector3 tempPoint = new Vector3(-point.z, point.y, point.x);
                float dot = diff.x * tempPoint.x /*+ diff.y + tempPoint.y */+ diff.z * tempPoint.z;
                if ( dot == 0 )
                {
                    onLine = true;
                }
                else
                {
                    atRight = dot > 0;
                    onLine = false;
                }
            }
        }
        return distance;
    }
}
