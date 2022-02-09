using UnityEngine;
using System;
using System.Collections;

public class UpdatableData : ScriptableObject
{
	public event Action OnDataValuesUpdated;

	public bool AutoUpdate;

	protected virtual void OnValidate()
	{
		if (AutoUpdate)
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
	}

	public void NotifyOfUpdatedValues()
	{
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		
		if (OnDataValuesUpdated != null)
			OnDataValuesUpdated();
	}
}
