using UnityEngine;
using System.Collections;

public class LowQualityCamera : MonoBehaviour 
{
	float shadowDistance;
	
	void OnPreRender () {
		shadowDistance = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = 0.0f;
	}
	
	void OnPostRender () {
		QualitySettings.shadowDistance = shadowDistance;
	}	
}