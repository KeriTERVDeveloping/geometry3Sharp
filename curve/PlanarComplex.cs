﻿using System;
using System.Collections.Generic;

namespace g3 
{

	public struct ComplexSegment2d
	{
		public Segment2d seg;
		public bool isClosed;
		public PlanarComplex.Element element;
	}
	public struct ComplexEndpoint2d
	{
		public Vector2d v;
		public bool isStart;
		public PlanarComplex.SmoothCurveElement element;
	}


	public class PlanarComplex 
	{
		// these determine pointwise sampling rates

		public double DistanceAccuracy = 0.1;
		public double AngleAccuracyDeg = 5.0;

		int id_generator = 1;

		public abstract class Element {
			public int ID = 0;

			public abstract IEnumerable<Segment2d> SegmentItr();
			public abstract AxisAlignedBox2d Bounds();
		}

		public class SmoothCurveElement : Element 
		{
			public IParametricCurve2d source;
			public PolyLine2d polyLine;

			public override IEnumerable<Segment2d> SegmentItr() {
				return polyLine.SegmentItr();
			}
			public override AxisAlignedBox2d Bounds() {
				return polyLine.GetBounds();
			}
		}

		public class SmoothLoopElement : Element 
		{
			public IParametricCurve2d source;
			public Polygon2d polygon;

			public override IEnumerable<Segment2d> SegmentItr() {
				return polygon.SegmentItr();
			}
			public override AxisAlignedBox2d Bounds() {
				return polygon.GetBounds();
			}
		}




		List<Element> vElements;


		public PlanarComplex() {
			vElements = new List<Element>();
		}


		public void Add(IParametricCurve2d curve) {
			if ( curve.IsClosed ) {
				SmoothLoopElement e = new SmoothLoopElement();
				e.ID = id_generator++;
				e.source = curve;
				UpdateSampling(e);
				vElements.Add(e);
			} else {
				SmoothCurveElement e = new SmoothCurveElement();
				e.ID = id_generator++;
				e.source = curve;
				UpdateSampling(e);
				vElements.Add(e);
			}
		}


		void UpdateSampling(SmoothCurveElement c) {
			c.polyLine = new PolyLine2d( 
                CurveSampler2.AutoSample(c.source, DistanceAccuracy) );
		}
		void UpdateSampling(SmoothLoopElement l) {
			l.polygon = new Polygon2d(
				CurveSampler2.AutoSample(l.source, DistanceAccuracy) );
		}


		public void Reverse(SmoothCurveElement c) {
			c.source.Reverse();
			UpdateSampling(c);
		}



		public IEnumerable<ComplexSegment2d> AllSegmentsItr() {
			foreach ( Element e in vElements ) {
				ComplexSegment2d s = new ComplexSegment2d();
				if ( e is SmoothLoopElement )
					s.isClosed = true;
				else if (e is SmoothCurveElement )
					s.isClosed = false;

				foreach ( Segment2d seg in e.SegmentItr() ) {
					s.seg = seg;
					s.element = e;
					yield return s;
				}
			}
		}


		public IEnumerable<SmoothLoopElement> LoopsItr() {
			foreach ( Element e in vElements ) {
				if ( e is SmoothLoopElement )
					yield return e as SmoothLoopElement;
			}
		}
		public IEnumerable<SmoothCurveElement> CurvesItr() {
			foreach ( Element e in vElements ) {
				if ( e is SmoothCurveElement )
					yield return e as SmoothCurveElement;
			}
		}

		// iterate through endpoints of open curves
		public IEnumerable<ComplexEndpoint2d> EndpointsItr() {
			foreach ( Element e in vElements ) {
				if ( e is SmoothCurveElement ) {
					SmoothCurveElement s = e as SmoothCurveElement;
					yield return new ComplexEndpoint2d() {
						v = s.polyLine.Start, isStart = true, element = s
					};
					yield return new ComplexEndpoint2d() {
						v = s.polyLine.End, isStart = false, element = s
					};
				}
			}
		}



		public AxisAlignedBox2d Bounds() {
			AxisAlignedBox2d box = AxisAlignedBox2d.Empty;
			foreach ( Element e in vElements ) {
				box.Contain(e.Bounds());
			}			
			return box;
		}





		public bool JoinElements(ComplexEndpoint2d a, ComplexEndpoint2d b) {
			if (a.element == b.element)
				throw new Exception("PlanarComplex.ChainElements: same curve!!");

			SmoothCurveElement c1 = a.element;
			SmoothCurveElement c2 = b.element;

			SmoothCurveElement joined = null;
			if ( a.isStart == false && b.isStart == true ) {
				vElements.Remove(c2);
				append(c1,c2);
				joined = c1;
			} else if ( a.isStart == true && b.isStart == false ) {
				vElements.Remove(c1);
				append(c2,c1);
				joined = c2;
			} else if (a.isStart == false) {		// end-to-end join
				c2.source.Reverse();
				vElements.Remove(c2);
				append(c1,c2);
				joined = c1;
			} else if (a.isStart == true) {		// start-to-start join
				c1.source.Reverse();
				vElements.Remove(c2);
				append(c1, c2);
				joined = c1;
			}

			if ( joined != null ) {
				// check if we have closed a loop
				double dDelta = ( joined.polyLine.Start - joined.polyLine.End ).Length;
				if ( dDelta < MathUtil.ZeroTolerance ) {
					SmoothLoopElement loop = new SmoothLoopElement() {
						ID = id_generator++, source = joined.source
					};
					vElements.Remove(joined);
					vElements.Add(loop);
					UpdateSampling(loop);
				}
				return true;
			}

			return false;
		}




		void append(SmoothCurveElement cTo, SmoothCurveElement cAppend) {
			ParametricCurveSequence2 use = null;
			if ( cTo.source is ParametricCurveSequence2 ) {
				use = cTo.source as ParametricCurveSequence2;
			} else {
				use = new ParametricCurveSequence2();
				use.Append( cTo.source );
			}

			if ( cAppend.source is ParametricCurveSequence2 ) {
				var cseq = cAppend.source as ParametricCurveSequence2;
				foreach ( var c in cseq.Curves )
					use.Append(c);
			} else {
				use.Append( cAppend.source );
			}

			cTo.source = use;
			UpdateSampling(cTo);
		}






		public void PrintStats() {
			System.Console.WriteLine("PlanarComplex Stats");
			List<SmoothLoopElement> Loops = new List<SmoothLoopElement>(LoopsItr());
			List<SmoothCurveElement> Curves = new List<SmoothCurveElement>(CurvesItr());

			System.Console.WriteLine("  Closed Loops: " + Loops.Count.ToString());
			System.Console.WriteLine("  Open Curves: " + Curves.Count.ToString());

			List<ComplexEndpoint2d> vEndpoints = new List<ComplexEndpoint2d>(EndpointsItr());
			System.Console.WriteLine("  Open Endpoints: " + vEndpoints.Count.ToString());
		}

	}
}
