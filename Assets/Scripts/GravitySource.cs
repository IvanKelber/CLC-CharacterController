using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
	public virtual Vector3 GetGravity (Vector3 position) {
		return Physics.gravity;
	}

    void OnEnable () {
		CustomGravity.RegisterSource(this);
	}

	void OnDisable () {
		CustomGravity.UnregisterSource(this);
	}
}
