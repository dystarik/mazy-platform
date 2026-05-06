namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class DelayNodeTests
{
    [Test]
    public async Task IsAwaiting_ReturnsFalse()
    {
        var node = new DelayNode(Guid.NewGuid(), 60);

        await Assert.That(node.IsAwaiting).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_FirstCall_SavesResumeAt_AndReturnsWait()
    {
        var node = new DelayNode(Guid.NewGuid(), 60);
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
        await Assert.That(context.Session.Variables.ContainsKey("__delay_resume_at")).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_BeforeResumeTime_ReturnsWait()
    {
        var futureTime = DateTime.UtcNow.AddMinutes(5).ToString("O");
        var node = new DelayNode(Guid.NewGuid(), 300);
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["__delay_resume_at"] = futureTime,
        });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
    }

    [Test]
    public async Task ExecuteAsync_AfterResumeTime_ReturnsContinue_AndRemovesKey()
    {
        var pastTime = DateTime.UtcNow.AddDays(-2).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        var node = new DelayNode(Guid.NewGuid(), 60);
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["__delay_resume_at"] = pastTime,
        });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables.ContainsKey("__delay_resume_at")).IsFalse();
    }

    private static ExecutionContext CreateContext(IDictionary<string, object?>? variables = null)
    {
        var session = new TestSession();

        if (variables is not null)
        {
            foreach (var (key, value) in variables)
            {
                session.Variables[key] = value;
            }
        }

        return new ExecutionContext
        {
            Session = session,
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
