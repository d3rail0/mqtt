﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Packets;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt
{
    internal class ConnectionProvider : IConnectionProvider
	{
        static readonly ITracer tracer = Tracer.Get<ConnectionProvider>();
        static readonly ConcurrentDictionary<string, IMqttChannel<IPacket>> connections;

		static ConnectionProvider ()
		{
			connections = new ConcurrentDictionary<string, IMqttChannel<IPacket>> ();
		}

		public int Connections { get { return connections.Skip (0).Count (); } }

		public IEnumerable<string> ActiveClients
		{
			get
			{
				return connections
					.Where (c => c.Value.IsConnected)
					.Select (c => c.Key);
			}
		}

		public void AddConnection (string clientId, IMqttChannel<IPacket> connection)
		{
			var existingConnection = default (IMqttChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				tracer.Warn (ServerProperties.Resources.ConnectionProvider_ClientIdExists, clientId);

				RemoveConnection (clientId);
			}

			connections.TryAdd (clientId, connection);
		}

		public IMqttChannel<IPacket> GetConnection (string clientId)
		{
			var existingConnection = default(IMqttChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				if (!existingConnection.IsConnected) {
					tracer.Warn (ServerProperties.Resources.ConnectionProvider_ClientDisconnected, clientId);

					RemoveConnection (clientId);
					existingConnection = default (IMqttChannel<IPacket>);
				}
			}

			return existingConnection;
		}

		public void RemoveConnection (string clientId)
		{
			var existingConnection = default (IMqttChannel<IPacket>);

			if (connections.TryRemove (clientId, out existingConnection)) {
				tracer.Info (ServerProperties.Resources.ConnectionProvider_RemovingClient, clientId);

				existingConnection.Dispose ();
			}
		}
	}
}
