using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GLDrawUtility : MonoBehaviour
{
//    public Action callback;
//    private static Material lineMaterial;
//    private static GLDrawUtility instance;
//
//    public static GLDrawUtility Instance {
//        get
//        {
//            if ( instance == null )
//            {
//                instance = Camera.main.GetComponent<GLDrawUtility>();
//                if ( instance == null ) {
//                    instance = Camera.main.gameObject.AddComponent<GLDrawUtility>();
//                }
//
//                lineMaterial = new Material("Shader \"Lines/Colored Blended\" { " +
//                                "SubShader { Pass { " +
//                                "Blend SrcAlpha OneMinusSrcAlpha " +
//                                 "ZTest Always " +
//                                "Lighting Off ZWrite Off Cull Off Fog { Mode Off } " +
//                                "BindChannels { " +
//                                "Bind \"vertex\", vertex Bind \"color\", color } " +
//                                "} } }");
//                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
//                lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
//            }
//
//            return instance;
//        }
//    }
//
//    void OnPostRender()
//    {
//        if ( callback != null )
//        {
//            GL.PushMatrix();
//            lineMaterial.SetPass(0);
//            callback();
//            GL.PopMatrix();
//        }
//    }
	
    public static void DrawLine(Vector3 from, Vector3 to)
    {
        GL.Begin(GL.LINES);
        GL.Vertex(from);
        GL.Vertex(to);
        GL.End();
    }

    public static void DrawLineSegments(IEnumerable<Vector3> segments)
    {
		CoupleEnumerator<Vector3> enumer = new CoupleEnumerator<Vector3>(segments);
        GL.Begin(GL.LINES);
		while (enumer.MoveNext()) {
			GL.Vertex(enumer.Previous);
			GL.Vertex(enumer.Current);
		}
        GL.End();
    }
	
    private const float dim2 = 0.7071068f;
    private const float dim3 = 0.5773503f;
    public static void DrawStar(float size = 1.0f)
    {
        float size2 = dim2 * size;
        float size3 = dim3 * size;

        GL.Begin(GL.LINES);

        GL.Vertex3(size, 0f, 0f);
        GL.Vertex3(-size, 0f, 0f);

        GL.Vertex3(0f, size, 0f);
        GL.Vertex3(0f, -size, 0f);

        GL.Vertex3(0f, 0f, size);
        GL.Vertex3(0f, 0f, -size);

        GL.Vertex3(size2, size2, 0f);
        GL.Vertex3(-size2, -size2, 0f);

        GL.Vertex3(size2, 0f, size2);
        GL.Vertex3(-size2, 0f, -size2);

        GL.Vertex3(0f, size2, size2);
        GL.Vertex3(0f, -size2, -size2);

        GL.Vertex3(size2, -size2, 0f);
        GL.Vertex3(-size2, size2, 0f);

        GL.Vertex3(size2, 0f, -size2);
        GL.Vertex3(-size2, 0f, size2);

        GL.Vertex3(0f, size2, -size2);
        GL.Vertex3(0f, -size2, size2);

        GL.Vertex3(size3, size3, size3);
        GL.Vertex3(-size3, -size3, -size3);

        GL.Vertex3(size3, size3, -size3);
        GL.Vertex3(-size3, -size3, size3);

        GL.Vertex3(size3, -size3, size3);
        GL.Vertex3(-size3, size3, -size3);

        GL.Vertex3(-size3, size3, size3);
        GL.Vertex3(size3, -size3, -size3);

        GL.End();
    }
	
	public static void DrawBox(Vector3 size) 
	{
		size = size * 0.5f;
		
		float xmax = +size.x;
		float xmin = -size.x;
		float ymax = +size.y;
		float ymin = -size.y;
		float zmax = +size.z;
		float zmin = -size.z;
		
		GL.Begin(GL.LINES);
		
		GL.Vertex3(xmin, ymin, zmin);
		GL.Vertex3(xmin, ymin, zmax);
		
		GL.Vertex3(xmin, ymin, zmin);
		GL.Vertex3(xmin, ymax, zmin);
		
		GL.Vertex3(xmin, ymin, zmin);
		GL.Vertex3(xmax, ymin, zmin);
		
		GL.Vertex3(xmin, ymax, zmax);
		GL.Vertex3(xmin, ymax, zmin);

		GL.Vertex3(xmin, ymax, zmax);
		GL.Vertex3(xmin, ymin, zmax);
		
		GL.Vertex3(xmin, ymax, zmax);
		GL.Vertex3(xmax, ymax, zmax);
		
		GL.Vertex3(xmax, ymax, zmin);
		GL.Vertex3(xmax, ymax, zmax);
		
		GL.Vertex3(xmax, ymax, zmin);
		GL.Vertex3(xmax, ymin, zmin);
		
		GL.Vertex3(xmax, ymax, zmin);
		GL.Vertex3(xmin, ymax, zmin);
		
		GL.Vertex3(xmax, ymin, zmax);
		GL.Vertex3(xmax, ymin, zmin);
		
		GL.Vertex3(xmax, ymin, zmax);
		GL.Vertex3(xmax, ymax, zmax);		
		
		GL.Vertex3(xmax, ymin, zmax);
		GL.Vertex3(xmin, ymin, zmax);
		
		GL.End();
	}
	
	public static void DrawArrow()
	{
		Vector3 arrowCap1 = Vector3.right - new Vector3(0.2f, 0.1f, 0f);
		Vector3 arrowCap2 = Vector3.right - new Vector3(0.2f, 0f, 0.1f);
		Vector3 arrowCap3 = Vector3.right - new Vector3(0.2f, -0.1f, 0f);
		Vector3 arrowCap4 = Vector3.right - new Vector3(0.2f, 0f, -0.1f);
		
		GL.Begin(GL.LINES);
		
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.right);
		
		GL.Vertex(Vector3.right);
		GL.Vertex(arrowCap1);
		
		GL.Vertex(Vector3.right);
		GL.Vertex(arrowCap2);
		
		GL.Vertex(Vector3.right);
		GL.Vertex(arrowCap3);
		
		GL.Vertex(Vector3.right);
		GL.Vertex(arrowCap4);
		
		GL.Vertex(arrowCap1);
		GL.Vertex(arrowCap2);
		
		GL.Vertex(arrowCap2);
		GL.Vertex(arrowCap3);
		
		GL.Vertex(arrowCap3);
		GL.Vertex(arrowCap4);
		
		GL.Vertex(arrowCap4);
		GL.Vertex(arrowCap1);
		
		GL.End();
	}
	
	public static void DrawTranslateGizmo()
	{
		GL.Color(Color.red);
		DrawArrow();
		
		GL.Color(Color.green);
		GL.PushMatrix();
		GL.modelview *= Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.forward), Vector3.one);
		DrawArrow();
		GL.PopMatrix();
		
		GL.Color(Color.blue);
		GL.PushMatrix();
		GL.modelview *= Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(-90f, Vector3.up), Vector3.one);
		DrawArrow();
		GL.PopMatrix();
	}
}
