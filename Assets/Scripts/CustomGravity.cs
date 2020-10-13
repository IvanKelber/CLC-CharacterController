using UnityEngine;
using System.Collections.Generic;

public static class CustomGravity
{
    public static HashSet<GravitySource> sources = new HashSet<GravitySource>();

    public static Vector3 GetGravity (Vector3 position) {
		Vector3 g = Vector3.zero;
		foreach(GravitySource source in sources) {
			g += source.GetGravity(position);
		}
		return g;
	}
	
	public static Vector3 GetGravity (Vector3 position, out Vector3 upAxis) {
		Vector3 g = GetGravity(position);
		upAxis = -g.normalized;
		return g;
	}
	
	public static Vector3 GetUpAxis (Vector3 position) {
		Vector3 g = Vector3.zero;
		foreach(GravitySource source in sources) {
			g += source.GetGravity(position);
		}
		return -g.normalized;
	}

    public static void RegisterSource(GravitySource source) {
        sources.Add(source);
    }

    public static void UnregisterSource(GravitySource source) {
        sources.Remove(source);
    }
}
