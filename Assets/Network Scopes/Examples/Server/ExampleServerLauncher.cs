
using UnityEngine;
using NetworkScopes;
using System.Reflection;
using System;
using System.Collections;
using System.Linq.Expressions;

public class ExampleServerLauncher : MonoBehaviour
{
	ExampleServer server;

	void Start()
	{
		server = new ExampleServer();

		server.StartServer(1234);
	}
}