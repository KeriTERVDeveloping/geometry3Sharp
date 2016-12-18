﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g3
{
    public interface ICurve
    {
        int VertexCount { get; }
        bool Closed { get; }

        Vector3d GetVertex(int i);

        IEnumerable<Vector3d> Vertices { get; }
    }




	public interface IParametricCurve2d
	{
		bool IsClosed {get;}

		// can call SampleT in range [0,ParamLength]
		double ParamLength {get;}
		Vector2d SampleT(double t);

		bool HasArcLength {get;}
		double ArcLength {get;}
		Vector2d SampleArcLength(double a);

		void Reverse();
	}
}
