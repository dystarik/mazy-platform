namespace MazyPlatform.Scenario.Nodes.Logic;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел задержки.
/// При первом вызове сохраняет время возобновления и ожидает.
/// При повторном вызове проверяет, прошло ли время.
/// Пробуждение обеспечивается внешним механизмом (например, отложенным сообщением или фоновым планировщиком).
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла задержки.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="seconds">Длительность задержки в секундах.</param>
public sealed class DelayNode(Guid nodeId, int seconds) : NodeBase(nodeId, "delay", isAwaiting: false)
{
    private const string _resumeAtKey = "__delay_resume_at";

    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var variables = context.Session.Variables;

        if (variables.TryGetValue(_resumeAtKey, out var resumeAtObj)
            && resumeAtObj is string resumeAtStr
            && DateTimeOffset.TryParse(
                resumeAtStr,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var resumeAt))
        {
            if (DateTimeOffset.UtcNow >= resumeAt)
            {
                variables.Remove(_resumeAtKey);
                return Task.FromResult(NodeResult.Continue());
            }

            return Task.FromResult(NodeResult.Wait());
        }

        var newResumeAt = DateTimeOffset.UtcNow.AddSeconds(seconds);
        variables[_resumeAtKey] = newResumeAt.ToString("O", CultureInfo.InvariantCulture);
        return Task.FromResult(NodeResult.Wait());
    }
}
