namespace MazyPlatform.Scenario.Validation;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

/// <summary>
/// Валидатор JSON сценария.
/// </summary>
/// <remarks>
/// Проверяет структуру графа и параметры узлов без создания runtime-экземпляров
/// узлов через <c>INodeDescriptor.Create</c>. Это позволяет использовать
/// валидатор перед сохранением или публикацией сценария.
/// </remarks>
public sealed class ScenarioValidator(IEnumerable<INodeSchemaProvider> schemaProviders) : IScenarioValidator
{
    private const string DefaultBranch = "default";
    private const string ConditionNodeType = "condition";
    private const string ReceiveButtonPressNodeType = "receive_button_press";
    private const string SwitchNodeType = "switch";

    private readonly IReadOnlyDictionary<string, INodeSchemaProvider> _schemaProviders =
        schemaProviders.ToDictionary(provider => provider.Type, StringComparer.Ordinal);

    /// <inheritdoc />
    public ScenarioValidationResult Validate(string scenarioJson)
    {
        var errors = new List<ScenarioValidationError>();

        if (string.IsNullOrWhiteSpace(scenarioJson))
        {
            errors.Add(Error(
                "scenario.empty",
                "JSON сценария не должен быть пустым.",
                path: "$"));
            return new ScenarioValidationResult(errors);
        }

        using var document = ParseDocument(scenarioJson, errors);
        return errors.Count > 0 ? new ScenarioValidationResult(errors) : ValidateRoot(document.RootElement, errors);
    }

    private ScenarioValidationResult ValidateRoot(JsonElement root, List<ScenarioValidationError> errors)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error(
                "scenario.root_invalid",
                "Корневой элемент сценария должен быть объектом.",
                path: "$"));
            return new ScenarioValidationResult(errors);
        }

        var hasStartNodeId = TryGetRequiredProperty(root, "startNodeId", "$.startNodeId", errors, out var startNodeIdElement);
        var hasNodes = TryGetRequiredProperty(root, "nodes", "$.nodes", errors, out var nodesElement);
        var hasConnections = TryGetRequiredProperty(root, "connections", "$.connections", errors, out var connectionsElement);

        var startNodeId = hasStartNodeId && TryReadGuid(startNodeIdElement, "$.startNodeId", errors, out var parsedStartNodeId)
            ? parsedStartNodeId
            : (Guid?)null;

        var nodes = hasNodes
            ? ReadNodes(nodesElement, errors)
            : [];
        var nodeIds = nodes
            .Where(static node => node.Id is not null)
            .Select(static node => node.Id!.Value)
            .ToHashSet();

        if (hasNodes && nodesElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error(
                "scenario.nodes_invalid",
                "Поле \"nodes\" должно быть массивом.",
                path: "$.nodes"));
        }

        if (hasConnections && connectionsElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error(
                "scenario.connections_invalid",
                "Поле \"connections\" должно быть массивом.",
                path: "$.connections"));
        }

        if (hasStartNodeId && startNodeId is not null && !nodeIds.Contains(startNodeId.Value))
        {
            errors.Add(Error(
                "scenario.start_node_not_found",
                $"Стартовый узел {startNodeId} не найден в nodes.",
                startNodeId,
                "$.startNodeId"));
        }

        using var emptyParamsDocument = JsonDocument.Parse("{}");
        ValidateNodes(nodes, emptyParamsDocument.RootElement, errors);

        var connections = hasConnections
            ? ReadConnections(connectionsElement, nodeIds, errors)
            : [];

        ValidateDuplicateBranches(connections, errors);
        ValidateBranchRules(nodes, connections, errors);
        ValidateDuplicateEntryPayloads(nodes, errors);
        ValidateDuplicateSwitchCaseValues(nodes, errors);

        return new ScenarioValidationResult(errors);
    }

    private JsonDocument ParseDocument(string scenarioJson, List<ScenarioValidationError> errors)
    {
        try
        {
            return JsonDocument.Parse(scenarioJson);
        }
        catch (JsonException ex)
        {
            errors.Add(Error(
                "scenario.invalid_json",
                $"JSON сценария не удалось разобрать: {ex.Message}",
                path: "$"));
            return JsonDocument.Parse("{}");
        }
    }

    private bool TryGetRequiredProperty(
        JsonElement parent,
        string propertyName,
        string path,
        List<ScenarioValidationError> errors,
        out JsonElement value)
    {
        if (parent.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        errors.Add(Error(
            "scenario.root_field_missing",
            $"В сценарии отсутствует обязательное поле \"{propertyName}\".",
            path: path));
        return false;
    }

    private bool TryReadGuid(
        JsonElement value,
        string path,
        List<ScenarioValidationError> errors,
        out Guid guid)
    {
        if (value.ValueKind == JsonValueKind.String && value.TryGetGuid(out guid))
        {
            return true;
        }

        guid = Guid.Empty;
        errors.Add(Error(
            "scenario.guid_invalid",
            $"Поле \"{path}\" должно быть GUID.",
            path: path));
        return false;
    }

    private List<RawScenarioNode> ReadNodes(JsonElement nodesElement, List<ScenarioValidationError> errors)
    {
        var nodes = new List<RawScenarioNode>();

        if (nodesElement.ValueKind != JsonValueKind.Array)
        {
            return nodes;
        }

        var seenNodeIds = new HashSet<Guid>();
        var index = 0;
        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var nodePath = $"$.nodes[{index}]";
            if (nodeElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error(
                    "scenario.node_invalid",
                    "Элемент nodes должен быть объектом.",
                    path: nodePath));
                index++;
                continue;
            }

            var nodeId = ReadNodeId(nodeElement, nodePath, seenNodeIds, errors);
            var nodeType = ReadNodeType(nodeElement, nodePath, nodeId, errors);
            var parameters = ReadNodeParameters(nodeElement, nodePath, nodeId, errors);

            nodes.Add(new RawScenarioNode(nodeId, nodeType, parameters, nodePath));
            index++;
        }

        return nodes;
    }

    private Guid? ReadNodeId(
        JsonElement nodeElement,
        string nodePath,
        HashSet<Guid> seenNodeIds,
        List<ScenarioValidationError> errors)
    {
        if (!nodeElement.TryGetProperty("id", out var idElement))
        {
            errors.Add(Error(
                "scenario.node_id_missing",
                "У узла отсутствует обязательное поле \"id\".",
                path: $"{nodePath}.id"));
            return null;
        }

        if (!TryReadGuid(idElement, $"{nodePath}.id", errors, out var nodeId))
        {
            return null;
        }

        if (!seenNodeIds.Add(nodeId))
        {
            errors.Add(Error(
                "scenario.duplicate_node_id",
                $"Узел с id {nodeId} объявлен больше одного раза.",
                nodeId,
                $"{nodePath}.id"));
        }

        return nodeId;
    }

    private string? ReadNodeType(
        JsonElement nodeElement,
        string nodePath,
        Guid? nodeId,
        List<ScenarioValidationError> errors)
    {
        if (!nodeElement.TryGetProperty("type", out var typeElement))
        {
            errors.Add(Error(
                "scenario.node_type_missing",
                "У узла отсутствует обязательное поле \"type\".",
                nodeId,
                $"{nodePath}.type"));
            return null;
        }

        if (typeElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            errors.Add(Error(
                "scenario.node_type_invalid",
                "Поле \"type\" должно быть непустой строкой.",
                nodeId,
                $"{nodePath}.type"));
            return null;
        }

        var nodeType = typeElement.GetString()!;
        if (!_schemaProviders.ContainsKey(nodeType))
        {
            errors.Add(Error(
                "scenario.unknown_node_type",
                $"Тип узла \"{nodeType}\" не зарегистрирован.",
                nodeId,
                $"{nodePath}.type"));
        }

        return nodeType;
    }

    private JsonElement? ReadNodeParameters(
        JsonElement nodeElement,
        string nodePath,
        Guid? nodeId,
        List<ScenarioValidationError> errors)
    {
        if (!nodeElement.TryGetProperty("params", out var parameters))
        {
            return null;
        }

        if (parameters.ValueKind == JsonValueKind.Object)
        {
            return parameters;
        }

        errors.Add(Error(
            "scenario.node_params_invalid",
            "Поле \"params\" должно быть объектом.",
            nodeId,
            $"{nodePath}.params"));
        return null;
    }

    private IReadOnlyList<RawConnection> ReadConnections(
        JsonElement connectionsElement,
        HashSet<Guid> nodeIds,
        List<ScenarioValidationError> errors)
    {
        var connections = new List<RawConnection>();

        if (connectionsElement.ValueKind != JsonValueKind.Array)
        {
            return connections;
        }

        var index = 0;
        foreach (var connectionElement in connectionsElement.EnumerateArray())
        {
            var connectionPath = $"$.connections[{index}]";
            if (connectionElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error(
                    "scenario.connection_invalid",
                    "Элемент connections должен быть объектом.",
                    path: connectionPath));
                index++;
                continue;
            }

            var fromNodeId = ReadConnectionNodeId(connectionElement, "from", connectionPath, nodeIds, errors);
            var toNodeId = ReadConnectionNodeId(connectionElement, "to", connectionPath, nodeIds, errors);
            var branchKey = ReadBranchKey(connectionElement, connectionPath, fromNodeId, errors);

            if (fromNodeId is not null && toNodeId is not null && branchKey is not null)
            {
                connections.Add(new RawConnection(fromNodeId.Value, toNodeId.Value, branchKey, connectionPath));
            }

            index++;
        }

        return connections;
    }

    private Guid? ReadConnectionNodeId(
        JsonElement connectionElement,
        string propertyName,
        string connectionPath,
        HashSet<Guid> nodeIds,
        List<ScenarioValidationError> errors)
    {
        var path = $"{connectionPath}.{propertyName}";

        if (!connectionElement.TryGetProperty(propertyName, out var nodeIdElement))
        {
            errors.Add(Error(
                "scenario.connection_node_missing",
                $"У связи отсутствует обязательное поле \"{propertyName}\".",
                path: path));
            return null;
        }

        if (!TryReadGuid(nodeIdElement, path, errors, out var nodeId))
        {
            return null;
        }

        if (!nodeIds.Contains(nodeId))
        {
            errors.Add(Error(
                "scenario.connection_node_not_found",
                $"Связь указывает на несуществующий узел {nodeId}.",
                nodeId,
                path));
        }

        return nodeId;
    }

    private string? ReadBranchKey(
        JsonElement connectionElement,
        string connectionPath,
        Guid? fromNodeId,
        List<ScenarioValidationError> errors)
    {
        if (!connectionElement.TryGetProperty("branch", out var branchElement))
        {
            return DefaultBranch;
        }

        if (branchElement.ValueKind != JsonValueKind.String)
        {
            errors.Add(Error(
                "scenario.connection_branch_invalid",
                "Поле \"branch\" должно быть строкой.",
                fromNodeId,
                $"{connectionPath}.branch"));
            return null;
        }

        var branchKey = branchElement.GetString();
        if (string.IsNullOrWhiteSpace(branchKey))
        {
            errors.Add(Error(
                "scenario.connection_branch_invalid",
                "Поле \"branch\" не должно быть пустым.",
                fromNodeId,
                $"{connectionPath}.branch"));
            return null;
        }

        return branchKey;
    }

    private void ValidateNodes(
        IReadOnlyList<RawScenarioNode> nodes,
        JsonElement emptyParameters,
        List<ScenarioValidationError> errors)
    {
        foreach (var node in nodes)
        {
            if (node is not { Id: { } nodeId, Type: { } nodeType })
            {
                continue;
            }

            if (!_schemaProviders.TryGetValue(nodeType, out var provider))
            {
                continue;
            }

            var parameters = node.Parameters ?? emptyParameters;
            foreach (var error in provider.Validate(nodeId, parameters))
            {
                errors.Add(Error(
                    "scenario.node_params_validation_failed",
                    error,
                    nodeId,
                    $"{node.Path}.params"));
            }
        }
    }

    private void ValidateDuplicateBranches(
        IReadOnlyList<RawConnection> connections,
        List<ScenarioValidationError> errors)
    {
        var branchesByNode = new Dictionary<Guid, HashSet<string>>();

        foreach (var connection in connections)
        {
            if (!branchesByNode.TryGetValue(connection.FromNodeId, out var branches))
            {
                branches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                branchesByNode[connection.FromNodeId] = branches;
            }

            if (!branches.Add(connection.BranchKey))
            {
                errors.Add(Error(
                    "scenario.duplicate_branch",
                    $"У узла {connection.FromNodeId} больше одной связи для ветки \"{connection.BranchKey}\".",
                    connection.FromNodeId,
                    $"{connection.Path}.branch"));
            }
        }
    }

    private void ValidateBranchRules(
        IReadOnlyList<RawScenarioNode> nodes,
        IReadOnlyList<RawConnection> connections,
        List<ScenarioValidationError> errors)
    {
        var nodesById = new Dictionary<Guid, RawScenarioNode>();
        foreach (var node in nodes)
        {
            if (node.Id is { } nodeId)
            {
                nodesById.TryAdd(nodeId, node);
            }
        }

        foreach (var connection in connections)
        {
            if (!nodesById.TryGetValue(connection.FromNodeId, out var sourceNode)
                || sourceNode.Type is null)
            {
                continue;
            }

            if (string.Equals(sourceNode.Type, ConditionNodeType, StringComparison.Ordinal))
            {
                ValidateConditionBranch(connection, errors);
                continue;
            }

            if (string.Equals(sourceNode.Type, SwitchNodeType, StringComparison.Ordinal))
            {
                ValidateSwitchBranch(sourceNode, connection, errors);
                continue;
            }

            if (!string.Equals(connection.BranchKey, DefaultBranch, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error(
                    "scenario.unexpected_branch",
                    $"Узел типа \"{sourceNode.Type}\" поддерживает только ветку \"default\".",
                    connection.FromNodeId,
                    $"{connection.Path}.branch"));
            }
        }
    }

    private void ValidateConditionBranch(RawConnection connection, List<ScenarioValidationError> errors)
    {
        if (string.Equals(connection.BranchKey, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(connection.BranchKey, "false", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        errors.Add(Error(
            "scenario.condition_branch_invalid",
            "Узел condition поддерживает только ветки \"true\" и \"false\".",
            connection.FromNodeId,
            $"{connection.Path}.branch"));
    }

    private void ValidateSwitchBranch(
        RawScenarioNode sourceNode,
        RawConnection connection,
        List<ScenarioValidationError> errors)
    {
        if (string.Equals(connection.BranchKey, DefaultBranch, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var branchKeys = ReadSwitchBranchKeys(sourceNode.Parameters);
        if (branchKeys.Contains(connection.BranchKey))
        {
            return;
        }

        errors.Add(Error(
            "scenario.switch_branch_invalid",
            $"Ветка \"{connection.BranchKey}\" не объявлена в switch.cases.",
            connection.FromNodeId,
            $"{connection.Path}.branch"));
    }

    private HashSet<string> ReadSwitchBranchKeys(JsonElement? parameters)
    {
        var branchKeys = new HashSet<string>(StringComparer.Ordinal);

        if (parameters is null
            || !parameters.Value.TryGetProperty("cases", out var casesElement)
            || casesElement.ValueKind != JsonValueKind.Array)
        {
            return branchKeys;
        }

        foreach (var caseElement in casesElement.EnumerateArray())
        {
            if (caseElement.ValueKind != JsonValueKind.Object
                || !caseElement.TryGetProperty("branchKey", out var branchKeyElement)
                || branchKeyElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var branchKey = branchKeyElement.GetString();
            if (!string.IsNullOrWhiteSpace(branchKey))
            {
                branchKeys.Add(branchKey);
            }
        }

        return branchKeys;
    }

    private void ValidateDuplicateEntryPayloads(
        IReadOnlyList<RawScenarioNode> nodes,
        List<ScenarioValidationError> errors)
    {
        var payloads = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            if (node is not { Id: { } nodeId, Type: ReceiveButtonPressNodeType, Parameters: { } parameters })
            {
                continue;
            }

            var isEntry = parameters.TryGetProperty("isEntry", out var isEntryElement)
                && isEntryElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                && isEntryElement.GetBoolean();

            if (!isEntry
                || !parameters.TryGetProperty("expectedPayloads", out var payloadsElement)
                || payloadsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var payloadElement in payloadsElement.EnumerateArray())
            {
                if (payloadElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var payload = payloadElement.GetString();
                if (string.IsNullOrWhiteSpace(payload))
                {
                    continue;
                }

                if (!payloads.TryAdd(payload, nodeId))
                {
                    errors.Add(Error(
                        "scenario.duplicate_entry_payload",
                        $"Payload \"{payload}\" объявлен как entry point в нескольких узлах: {payloads[payload]} и {nodeId}.",
                        nodeId,
                        $"{node.Path}.params.expectedPayloads"));
                }
            }
        }
    }

    private void ValidateDuplicateSwitchCaseValues(
        IReadOnlyList<RawScenarioNode> nodes,
        List<ScenarioValidationError> errors)
    {
        foreach (var node in nodes)
        {
            if (node is not { Id: { } nodeId, Type: SwitchNodeType, Parameters: { } parameters }
                || !parameters.TryGetProperty("cases", out var casesElement)
                || casesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var caseValues = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var caseElement in casesElement.EnumerateArray())
            {
                if (caseElement.ValueKind == JsonValueKind.Object
                    && caseElement.TryGetProperty("value", out var valueElement)
                    && valueElement.ValueKind == JsonValueKind.String)
                {
                    var value = valueElement.GetString() ?? string.Empty;
                    if (!caseValues.Add(value))
                    {
                        errors.Add(Error(
                            "scenario.duplicate_switch_case_value",
                            $"В switch-узле значение cases.value \"{value}\" объявлено больше одного раза.",
                            nodeId,
                            $"{node.Path}.params.cases[{index}].value"));
                    }
                }

                index++;
            }
        }
    }

    private ScenarioValidationError Error(
        string code,
        string message,
        Guid? nodeId = null,
        string? path = null) =>
        new(code, message, nodeId, path);

    private sealed record RawScenarioNode(
        Guid? Id,
        string? Type,
        JsonElement? Parameters,
        string Path);

    private sealed record RawConnection(
        Guid FromNodeId,
        Guid ToNodeId,
        string BranchKey,
        string Path);
}
