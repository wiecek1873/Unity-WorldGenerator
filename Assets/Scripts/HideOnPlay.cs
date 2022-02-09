using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public class HideOnPlay : MonoBehaviour
{
	void Start()
	{
		gameObject.SetActive(false);
	}
}
