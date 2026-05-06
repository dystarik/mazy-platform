namespace MazyPlatform.Scenario.Registration;

using System.Reflection;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Сканер сборок на предмет реализаций <see cref="INodeDescriptor"/>.
/// Используется и в core-сборке, и в платформенных сборках для регистрации дескрипторов узлов.
/// </summary>
public static class NodeDescriptorScanner
{
    /// <summary>
    /// Сканирует указанную сборку, находит все реализации <see cref="INodeDescriptor"/>
    /// и регистрирует их в DI как singleton по трём типам:
    /// конкретный тип, <see cref="INodeSchemaProvider"/>, <see cref="INodeDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// Дескрипторы должны быть top-level не-generic классами (могут быть как public, так и internal).
    /// Если в сборке есть nested или generic типы, реализующие <see cref="INodeDescriptor"/>,
    /// метод бросает <see cref="InvalidOperationException"/> с перечислением таких типов и причинами.
    /// </remarks>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="assembly">Сканируемая сборка.</param>
    public static void RegisterDescriptorsFromAssembly(IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var descriptorTypes = assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(INodeDescriptor).IsAssignableFrom(t))
            .ToList();

        var invalidDescriptorTypes = descriptorTypes
            .Where(t => t.IsNested || t.IsGenericTypeDefinition || t.ContainsGenericParameters)
            .Select(static t =>
            {
                var reasons = new List<string>(2);

                if (t.IsNested)
                    reasons.Add("nested");

                if (t.IsGenericTypeDefinition || t.ContainsGenericParameters)
                    reasons.Add("generic");

                return $"  - {t.FullName ?? t.Name} ({string.Join(", ", reasons)})";
            })
            .ToList();

        if (invalidDescriptorTypes.Count > 0)
        {
            throw new InvalidOperationException(
                "Дескрипторы должны быть top-level не-generic классами.\n" +
                "Найдены неподдерживаемые типы:\n" +
                string.Join('\n', invalidDescriptorTypes));
        }

        foreach (var descriptorType in descriptorTypes.Where(descriptorType => services.All(s => s.ServiceType != descriptorType)))
        {
            services.AddSingleton(descriptorType);
            services.AddSingleton(
                typeof(INodeSchemaProvider),
                sp =>
                {
                    var provider = (INodeSchemaProvider)sp.GetRequiredService(descriptorType);

                    if (provider is NodeDescriptorBase baseDescriptor)
                    {
                        NodeDescriptorBase.EnsureSchemaValid(baseDescriptor.Type, baseDescriptor.Schema);
                    }

                    return provider;
                });
            services.AddSingleton(
                typeof(INodeDescriptor),
                sp =>
                {
                    var descriptor = (INodeDescriptor)sp.GetRequiredService(descriptorType);

                    if (descriptor is NodeDescriptorBase baseDescriptor)
                    {
                        NodeDescriptorBase.EnsureSchemaValid(baseDescriptor.Type, baseDescriptor.Schema);
                    }

                    return descriptor;
                });
        }
    }
}
