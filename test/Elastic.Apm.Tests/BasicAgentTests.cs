using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Elastic.Apm.Tests.Mocks;
using Xunit;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]


namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Very basic agent related tests
	/// </summary>
	public class BasicAgentTests
	{
		/// <summary>
		/// Creates a simple transaction.
		/// Makes sure that the agent reports the transaction with the correct agent version,
		/// which is the version of the Elastic.Apm assembly.
		/// </summary>
		[Fact]
		public void AgentVersion()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
				agent.Tracer.CaptureTransaction("TestName", "TestType", () => { Thread.Sleep(5); });

			Assert.Equal(Assembly.Load("Elastic.Apm").GetName().Version.ToString(), payloadSender.Payloads[0].Service.Agent.Version);
		}

		/// <summary>
		/// Starts a custom span with name length > 1024.
		/// Makes sure that the name is truncated.
		/// Reason: server rejects spans with name length > 1024.
		/// </summary>
		[Fact]
		public void SpanNameLengthTest()
		{
			var spanName = new StringBuilder();

			for (var i = 0; i < 1030; i++) spanName.Append('a');

			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction("TestTransaction", "Test", (t) => { t.CaptureSpan(spanName.ToString(), "test", () => { }); });

			Assert.NotNull(payloadSender.FirstSpan);
			Assert.Equal(1024, payloadSender.FirstSpan.Name.Length);
			Assert.Equal(spanName.ToString(0, 1021), payloadSender.FirstSpan.Name.Substring(0, 1021));
			Assert.Equal("...", payloadSender.FirstSpan.Name.Substring(1021, 3));
		}
	}
}
