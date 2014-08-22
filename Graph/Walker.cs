#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace AI
{
	public class Walker
	{
		public interface IGuide
		{
			Line GetNextLine(Walker walker);
			Line GetPrevLine(Walker walker);
		}

		protected bool isValid = true;
		protected IGuide guide;
		protected Point point;
		protected Mark mark;
		protected float distance;
		protected float direction;
		protected float maxDistance;

		public Walker(IGuide aguide, Mark amark)
		{
			guide = aguide;
			mark = amark;
			point = mark.Point;
			distance = 0f;
		}
		public Walker(IGuide aguide, Point apoint)
		{
			guide = aguide;
			mark = null;
			point = apoint;
			distance = 0f;
		}

		public bool Valid { get { return isValid; } }
		public Point Point { get { return this.point; } }
		public Line Line { get { return this.point.line; } }
		public Graph Graph { get { return this.point.line.graph; } }
		public float Distance { get { return distance; } }
		public Mark Mark { get { return mark; } }
		public bool Direction { get { return direction > 0f; } set { direction = value ? 1f : 0f; } }
		//
		public Vector3 Next { get { return direction > 0f ? point.Next : point.Prev; } }
		public Vector3 Prev { get { return direction > 0f ? point.Prev : point.Next; } }
		public Vector3 Orientation { get { return direction > 0f ? point.Next - point.Prev : point.Prev - point.Next; } }
		public float DistanceToNext { get { return direction > 0f ? point.DistanceToNext : -point.DistanceToPrev; } }
		public float DistanceToPrev { get { return direction > 0f ? point.DistanceToPrev : -point.DistanceToNext; } }
		public float DistanceToStart { get { return direction > 0f ? point.DistanceToStart : -point.DistanceToEnd; } }
		public float DistanceToEnd { get { return direction > 0f ? point.DistanceToEnd : -point.DistanceToStart; } }

		public float ResetDistance()
		{
			var result = distance;
			distance = 0f;
			return result;
		}
		public virtual void Move(float adistance)
		{
			var distanceResult = distance + adistance;

			if (adistance > 0f) {
				float nextDistance = DistanceToEnd;
				while (isValid && nextDistance < adistance) {
					adistance -= nextDistance;
					MoveToEnd();
					nextDistance = DistanceToEnd;
				}

				nextDistance = DistanceToNext;
				while (nextDistance < adistance) {
					adistance -= nextDistance;
					MoveToNext();
					nextDistance = DistanceToNext;
				}

			} else {
				float prevDistance = DistanceToStart;
				while (isValid && prevDistance > adistance) {
					adistance += prevDistance;
					MoveToStart();
					prevDistance = DistanceToStart;
				}

				prevDistance = DistanceToPrev;
				while (prevDistance > adistance) {
					adistance += prevDistance;
					MoveToPrev();
					prevDistance = DistanceToPrev;
				}
			}

			distance = distanceResult;
			adistance *= direction;
			point.normalized = adistance > 0f ? adistance / point.SegmentLength : (1f + adistance) / point.SegmentLength;
		}
		public virtual void Jump(float adistance)
		{
			Vector3 center = point.Position;
			if (adistance > 0f) {
				while (isValid) {
					float result = Geometry.CircleIntersectionOnLine(center, adistance, Prev, Next);
					if (result >= 0f && result <= 1f) {
						point.normalized = result;
						return;
					}
					MoveToNext();
				}
			} else {
				adistance = -adistance;
				while (isValid) {
					float result = Geometry.CircleIntersectionOnLine(center, adistance, Prev, Next);
					if (result >= 0f && result <= 1f) {
						point.normalized = result;
						return;
					}
					MoveToPrev();
				}
			}
		}
		public virtual void MoveToNext()
		{
			if (direction > 0f) {
				MoveToNextBase();
			} else {
				MoveToPrevBase();
			}
		}
		public virtual void MoveToPrev()
		{
			if (direction > 0f) {
				MoveToPrevBase();
			} else {
				MoveToNextBase();
			}
		}
		public virtual void MoveToEnd()
		{
			if (direction > 0f) {
				MoveToEndBase();
			} else {
				MoveToStartBase();
			}
		}
		public virtual void MoveToStart()
		{
			if (direction > 0f) {
				MoveToStartBase();
			} else {
				MoveToEndBase();
			}
		}
		//
		protected void MoveToNextBase()
		{
			if (point.AtLastSegment) {
				MoveToEndBase();
			} else {
				mark = null;
				distance += direction * point.DistanceToNext;
				point.segment++;
				point.normalized = 0f;
			}
		}
		protected void MoveToPrevBase()
		{
			if (point.AtFirstSegment) {
				MoveToStartBase();
			} else {
				mark = null;
				distance += direction * point.DistanceToPrev;
				point.segment--;
				point.normalized = 1f;
			}
		}
		protected void MoveToEndBase()
		{
			if (!isValid) {
				return;
			}

			mark = null;
			distance += direction * point.DistanceToEnd;
			var nextLine = guide.GetNextLine(this);
			if (nextLine == null) {
				isValid = false;
				throw new Exception("Next line should not be null");
			} else if (point.line.end == nextLine.start) {
				point.line = nextLine;
				point.segment = 0;
				point.normalized = 0f;
			} else if (point.line.end == nextLine.end) {
				point.line = nextLine;
				point.segment = nextLine.VertexCount;
				point.normalized = 1f;
				direction = -direction;
			} else {
				isValid = false;
				throw new Exception("Next line is not valid");
			}
		}
		protected void MoveToStartBase()
		{
			if (!isValid) {
				return;
			}

			mark = null;
			distance += direction * point.DistanceToStart;
			var prevLine = guide.GetPrevLine(this);
			if (prevLine == null) {
				isValid = false;
				throw new Exception("Previous line should not be null");
			} else if (point.line.start == prevLine.end) {
				point.line = prevLine;
				point.segment = prevLine.VertexCount;
				point.normalized = 1f;
			} else if (point.line.start == prevLine.start) {
				point.line = prevLine;
				point.segment = 0;
				point.normalized = 0f;
				direction = -direction;
			} else {
				isValid = false;
				throw new Exception("Previous line is not valid");
			}
		}
		//		protected void MoveToNextMarkBase(Predicate<Mark> filter = null)
		//		{
		//			var oldPoint = point;
		//			var oldDistance = distance;
		//			mark = mark == null ? point.line.NextMarkOf(point) : mark.NextMark;
		//
		//			while ((mark == null || (filter != null && !filter(mark))) && distance < maxDistance) {
		//				MoveToNextLine();
		//				mark = point.line.Marks.First;
		//			}
		//
		//			if (mark != null && distance + (mark.Point - point) < maxDistance) {
		//				MoveToPoint(mark.Point);
		//			} else {
		//				mark = null;
		//				point = oldPoint;
		//				distance = oldDistance;
		//			}
		//		}
		//		protected void MoveToPrevMarkBase(Predicate<Mark> filter = null)
		//		{
		//			var oldPoint = point;
		//			var oldDistance = distance;
		//			maxDistance *= -1f;
		//			mark = mark == null ? point.line.PrevMarkOf(point) : mark.PrevMark;
		//
		//			while ((mark == null || (filter != null && !filter(mark))) && distance > maxDistance) {
		//				MoveToPrevLine();
		//				mark = point.line.Marks.Last;
		//			}
		//
		//			if (mark != null && distance + (mark.Point - point) > maxDistance) {
		//				MoveToPoint(mark.Point);
		//			} else {
		//				mark = null;
		//				point = oldPoint;
		//				distance = oldDistance;
		//			}
		//		}
	}
}