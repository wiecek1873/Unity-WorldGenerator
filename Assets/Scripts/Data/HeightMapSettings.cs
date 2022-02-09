using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
	public NoiseSettings NoiseSettings;
	public bool UseFalloff; //todo not implemented yet
	public float HeightMultiplier;
	public AnimationCurve HeightCurve;

	public float MinHeight => HeightMultiplier * HeightCurve.Evaluate(0);

	public float MaxHeight => HeightMultiplier * HeightCurve.Evaluate(1);

	protected override void OnValidate()
	{
		NoiseSettings.ValidateValues();
		base.OnValidate();
	}
}
