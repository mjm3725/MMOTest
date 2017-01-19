using System;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;

namespace MMOServer
{
	class Program
	{
		static void Main(string[] args)
		{
			var bootstrap = BootstrapFactory.CreateBootstrap();

			if (!bootstrap.Initialize())
			{
				return;
			}

			if (bootstrap.Start() == StartResult.Failed)
			{
				return;
			}

			while (Console.ReadKey().KeyChar != 'q')
			{
				Console.WriteLine();
			}

			Console.WriteLine();
			bootstrap.Stop();
		}
	}
}
