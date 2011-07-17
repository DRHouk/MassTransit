// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Tests
{
	using System;
	using Exceptions;
	using Magnum.Extensions;
	using Magnum.TestFramework;
	using Messages;
	using NUnit.Framework;
	using TextFixtures;

	[TestFixture]
	public class Publishing_a_simple_request :
		LoopbackLocalAndRemoteTestFixture
	{
		[Test]
		public void Should_use_a_clean_syntax_following_standard_conventions()
		{
			var pongReceived = new FutureMessage<PongMessage>();
			var pingReceived = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(x =>
				{
					pingReceived.Set(x);
					RemoteBus.MessageContext<PingMessage>().Respond(new PongMessage(x.CorrelationId));
				});

			var ping = new PingMessage();

			var timeout = 8.Seconds();

			LocalBus.PublishRequest(ping, x =>
				{
					x.Handle<PongMessage>(message =>
						{
							message.CorrelationId.ShouldEqual(ping.CorrelationId, "The response correlationId did not match");
							pongReceived.Set(message);
						});

					x.SetTimeout(timeout);
				});

			pingReceived.IsAvailable(timeout).ShouldBeTrue("The ping was not received");
			pongReceived.IsAvailable(timeout).ShouldBeTrue("The pong was not received");
		}

		[Test]
		public void Should_support_send_as_well()
		{
			var pongReceived = new FutureMessage<PongMessage>();
			var pingReceived = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(x =>
				{
					pingReceived.Set(x);
					RemoteBus.MessageContext<PingMessage>().Respond(new PongMessage(x.CorrelationId));
				});

			var ping = new PingMessage();

			var timeout = 8.Seconds();

			RemoteBus.Endpoint.SendRequest(ping, LocalBus, x =>
				{
					x.Handle<PongMessage>(message =>
						{
							message.CorrelationId.ShouldEqual(ping.CorrelationId, "The response correlationId did not match");
							pongReceived.Set(message);
						});

					x.SetTimeout(timeout);
				});

			pingReceived.IsAvailable(timeout).ShouldBeTrue("The ping was not received");
			pongReceived.IsAvailable(timeout).ShouldBeTrue("The pong was not received");
		}

		[Test]
		public void Should_throw_a_timeout_exception_if_no_response_received()
		{
			var pongReceived = new FutureMessage<PongMessage>();
			var pingReceived = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(pingReceived.Set);

			var ping = new PingMessage();

			var timeout = 2.Seconds();

			Assert.Throws<RequestTimeoutException>(() =>
				{
					LocalBus.PublishRequest(ping, x =>
						{
							x.Handle<PongMessage>(pongReceived.Set);

							x.SetTimeout(timeout);
						});
				}, "A timeout exception should have been thrown");

			pingReceived.IsAvailable(timeout).ShouldBeTrue("The ping was not received");
			pongReceived.IsAvailable(timeout).ShouldBeFalse("The pong should not have been received");
		}

		[Test]
		public void Should_throw_a_handler_exception_on_the_calling_thread()
		{
			var pongReceived = new FutureMessage<PongMessage>();
			var pingReceived = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
			{
				pingReceived.Set(message);
				RemoteBus.MessageContext<PingMessage>().Respond(new PongMessage(message.CorrelationId));
			});

			var ping = new PingMessage();

			var timeout = 8.Seconds();

			var exception = Assert.Throws<RequestException>(() =>
				{
					LocalBus.PublishRequest(ping, x =>
						{
							x.Handle<PongMessage>(message =>
								{
									pongReceived.Set(message);

									throw new InvalidOperationException("I got it, but I am naughty with it.");
								});

							x.SetTimeout(timeout);
						});
				}, "A request exception should have been thrown");

			exception.Response.ShouldBeAnInstanceOf<PongMessage>();
			exception.InnerException.ShouldBeAnInstanceOf<InvalidOperationException>();

			pingReceived.IsAvailable(timeout).ShouldBeTrue("The ping was not received");
			pongReceived.IsAvailable(timeout).ShouldBeTrue("The pong was not received");
		}
	}
}