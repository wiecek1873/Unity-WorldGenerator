using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (Preview))]
public class MapPreviewEditor : Editor {

	public override void OnInspectorGUI() {
		Preview mapPreview = (Preview)target;

		if (DrawDefaultInspector ()) {
			if (mapPreview.AutoUpdate) {
				mapPreview.DrawPreviewEditor ();
			}
		}

		if (GUILayout.Button ("Generate")) {
			mapPreview.DrawPreviewEditor ();
		}
	}
}
