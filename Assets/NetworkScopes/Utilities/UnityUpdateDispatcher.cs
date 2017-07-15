using System;
using UnityEngine;

namespace NetworkScopes.Utilities
{
	public class UnityUpdateDispatcher : MonoBehaviour
	{
		private static UnityUpdateDispatcher _globalDispatcher;

		private static event Action _onUpdate = delegate { };

		public static event Action OnUpdate
		{
			add
			{
				_onUpdate += value;

				if (_globalDispatcher == null)
					_globalDispatcher = CreateGlobalDispatcher();
			}
			remove { _onUpdate -= value; }
		}

		static UnityUpdateDispatcher CreateGlobalDispatcher()
		{
			UnityUpdateDispatcher dispatcher = new GameObject("UnityUpdateDispatcher").AddComponent<UnityUpdateDispatcher>();
			return dispatcher;
		}

		void Update()
		{
			_onUpdate();
		}
	}
}