using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadHandler : MonoBehaviour
{
	private void OnThreadEnd(Func<object> generateData, Action<object> callback)
	{
		object data = generateData();
		lock (m_dataQueue) { m_dataQueue.Enqueue(new ThreadInfo(callback, data)); }
	}
	public static void RequestData(Func<object> generateData, Action<object> callback)
	{
		ThreadStart threadStart = delegate { instance.OnThreadEnd(generateData, callback); };
		new Thread(threadStart).Start();
	}

	struct ThreadInfo
	{
		public readonly Action<object> Callback;
		public readonly object InfoParameter;
		public ThreadInfo(Action<object> callback, object parameter)
		{
			Callback = callback;
			InfoParameter = parameter;
		}
	}
	static ThreadHandler instance;

	private Queue<ThreadInfo> m_dataQueue = new Queue<ThreadInfo>();


	private void Awake()
	{
		instance = FindObjectOfType<ThreadHandler>();
	}

	private void Update()
	{
		if (m_dataQueue.Count <= 0)
			return;
		for (int i = 0; i < m_dataQueue.Count; i++)
		{
			ThreadInfo threadInfo = m_dataQueue.Dequeue();
			threadInfo.Callback(threadInfo.InfoParameter);
		}
	}

}
