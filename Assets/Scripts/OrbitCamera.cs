using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    [SerializeField]
	Transform focus = default;

	[SerializeField, Range(1f, 20f)]
	float distance = 5f;

    [SerializeField, Min(0)]
    float focusRadius = 1;

    [SerializeField, Range(0,1)]
    float focusCentering = .5f;

    [SerializeField, Range(1,360)]
    float rotationSpeed = 90;

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30, maxVerticalAngle = 60;

    [SerializeField, Min(0)]
    float alignDelay = 5;

    [SerializeField, Range(0f, 90f)]
	float alignSmoothRange = 45f;

    [SerializeField, Min(0f)]
	float upAlignmentSpeed = 360f;

    [SerializeField]
    LayerMask obstructionMask;

    float lastManualRotationTime;
    Vector3 focusPoint, previousFocusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f);

	Quaternion gravityAlignment = Quaternion.identity;
    Quaternion orbitRotation;

    Camera camera;

    Vector3 CameraHalfExtends {
		get {
			Vector3 halfExtends;
			halfExtends.y =
				camera.nearClipPlane *
				Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView);
			halfExtends.x = halfExtends.y * camera.aspect;
			halfExtends.z = 0f;
			return halfExtends;
		}
	}

    void OnValidate () {
		if (maxVerticalAngle < minVerticalAngle) {
			maxVerticalAngle = minVerticalAngle;
		}
	}

    void Awake()
    {
        focusPoint = focus.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
        camera = GetComponent<Camera>();
    }

    void LateUpdate() {

        UpdateGravityAlignment();
        UpdateFocusPoint();
        if(ManualRotation() || AutomaticRotation()) {
            ConstrainAngles();
            orbitRotation = Quaternion.Euler(orbitAngles);
        } 
        Quaternion lookRotation = gravityAlignment * orbitRotation;

        Vector3 lookDirection = transform.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * camera.nearClipPlane;
		Vector3 rectPosition = lookPosition + rectOffset;
		Vector3 castFrom = focus.position;
		Vector3 castLine = rectPosition - castFrom;
		float castDistance = castLine.magnitude;
		Vector3 castDirection = castLine / castDistance;
		if (Physics.BoxCast(
			castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask
		)) {
            rectPosition = castFrom + castDirection * hit.distance;

			lookPosition = rectPosition - rectOffset;
		}

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateGravityAlignment () {
		Vector3 fromUp = gravityAlignment * Vector3.up;
		Vector3 toUp = CustomGravity.GetUpAxis(focusPoint);
		float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1, 1);
		float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
		float maxAngle = upAlignmentSpeed * Time.deltaTime;
		Quaternion newAlignment =
			Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;

        if(angle <= maxAngle) {
		    gravityAlignment = newAlignment;
        }
		else {
			gravityAlignment = Quaternion.SlerpUnclamped(
				gravityAlignment, newAlignment, maxAngle / angle
			);
		}
	}

    void UpdateFocusPoint() {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if(focusRadius > 0) {
            float t = 1;
            float distToFocus = Vector3.Distance(targetPoint, focusPoint);
            if(distToFocus > .01 && focusCentering > 0) {
                t = EaseDown(Time.unscaledDeltaTime);
            }
            if(distToFocus > focusRadius) {
                t = Mathf.Min(t, focusRadius / distToFocus);
            } 
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        } else {
            focusPoint = targetPoint;
        }
    }

	void ConstrainAngles () {
		orbitAngles.x =
			Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

		if (orbitAngles.y < 0f) {
			orbitAngles.y += 360f;
		}
		else if (orbitAngles.y >= 360f) {
			orbitAngles.y -= 360f;
		}
	}

    bool ManualRotation () {
		Vector2 input = new Vector2(
			Input.GetAxis("Vertical Camera"),
			Input.GetAxis("Horizontal Camera")
		);
		const float e = 0.001f;
		if (input.x < -e || input.x > e || input.y < -e || input.y > e) {
			orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
		}
        return false;
	}

    bool AutomaticRotation() {
        if(Time.unscaledTime - lastManualRotationTime < alignDelay) {
            return false;
        }
        //Get camera movement
        Vector3 alignedDelta =
			Quaternion.Inverse(gravityAlignment) *
			(focusPoint - previousFocusPoint);
		Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);
        
		float movementDeltaSqr = movement.sqrMagnitude;
		if (movementDeltaSqr < 0.000001f) {
			return false;
		}
        //Step horizontal camera rotation according to movement
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        if (deltaAbs < alignSmoothRange) {
			rotationChange *= deltaAbs / alignSmoothRange;
		}
		else if (180f - deltaAbs < alignSmoothRange) {
			rotationChange *= (180f - deltaAbs) / alignSmoothRange;
		}
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);

        return true;
    }

	static float GetAngle (Vector2 direction) {
		float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
		return direction.x < 0f ? 360f - angle : angle;
    }

    float EaseDown(float t) {
        return Mathf.Pow(1 - focusCentering, t);
    }

}
