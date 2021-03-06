﻿// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using log4net;
	using Util;

	public class ServiceContainer :
		IServiceContainer
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (ServiceContainer));
		readonly IServiceBus _bus;
		readonly ServiceCatalog _catalog;
		bool _disposed;

		public ServiceContainer(IServiceBus bus)
		{
			_bus = bus;
			_catalog = new ServiceCatalog();
		}

		public void AddService(BusServiceLayer layer, IBusService service)
		{
			_catalog.Add(layer, service);
		}

		public void Start()
		{
			IList<IBusService> started = new List<IBusService>();

			foreach (IBusService service in _catalog.Services)
			{
				try
				{
					_log.DebugFormat("Starting bus service: {0}", service.GetType().ToFriendlyName());

					service.Start(_bus);
					started.Add(service);
				}
				catch (Exception ex)
				{
					_log.Error("Failed to start bus service: " + service.GetType().ToFriendlyName(), ex);

					foreach (IBusService stopService in started)
					{
						try
						{
							stopService.Stop();
						}
						catch (Exception stopEx)
						{
							_log.Warn("Failed to stop a service that was started during a failed bus startup: " +
							          stopService.GetType().ToFriendlyName(), stopEx);
						}
					}
				}
			}
		}

		public void Stop()
		{
			foreach (IBusService service in _catalog.Services.Reverse())
			{
				try
				{
					service.Stop();
				}
				catch (Exception ex)
				{
					_log.Error("Failed to stop service: " + service.GetType().ToFriendlyName(), ex);
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				foreach (IBusService service in _catalog.Services.Reverse())
				{
					service.Dispose();
				}
			}
			_disposed = true;
		}

		~ServiceContainer()
		{
			Dispose(false);
		}
	}
}