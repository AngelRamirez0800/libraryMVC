using System.Reflection;
using libraryMVC.Models;
using Xunit;

namespace libraryMVC.Tests.Unit.Architecture;

public sealed class ApplicationContractTests
{
    private const string InterfacesNamespace = "libraryMVC.Interfaces";
    private const string AbstractionsNamespace = "libraryMVC.Abstractions";
    private static readonly Assembly ApplicationAssembly = typeof(Book).Assembly;

    [Fact]
    public void PublicApplicationLayersAreExposedAsPublicAssemblyTypes()
    {
        string[] applicationNamespaces =
        [
            "libraryMVC.Controllers",
            "libraryMVC.DTOs",
            "libraryMVC.Models",
            "libraryMVC.Services",
            InterfacesNamespace,
            AbstractionsNamespace
        ];
        Type[] layerTypes = ApplicationAssembly.GetExportedTypes()
            .Where(type => applicationNamespaces.Contains(type.Namespace))
            .ToArray();

        Assert.NotEmpty(layerTypes);
        Assert.All(layerTypes, type => Assert.True(type.IsPublic || type.IsNestedPublic));
        Assert.All(applicationNamespaces, namespaceName =>
            Assert.Contains(layerTypes, type => type.Namespace == namespaceName));
    }

    [Fact]
    public void PublicServiceTypesImplementApplicationInterfaces()
    {
        Type[] serviceTypes = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.Namespace == "libraryMVC.Services" && type.IsClass && !type.IsAbstract)
            .ToArray();

        Assert.NotEmpty(serviceTypes);
        Assert.All(
            serviceTypes,
            service => Assert.Contains(
                service.GetInterfaces(),
                implemented => implemented.Namespace == InterfacesNamespace));
    }

    [Fact]
    public void PublicApplicationInterfacesHaveConcreteImplementations()
    {
        Type[] contracts = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.IsInterface && IsApplicationContract(type))
            .ToArray();
        Type[] implementations = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .ToArray();

        Assert.NotEmpty(contracts);

        Type[] orphanContracts = contracts
            .Where(contract => !implementations.Any(implementation =>
                ImplementsContract(implementation, contract)))
            .ToArray();

        Assert.Empty(orphanContracts);
    }

    [Fact]
    public void ControllerAndServiceDependenciesHaveConcreteImplementations()
    {
        Type[] implementations = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .ToArray();
        Type[] controllers = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.Namespace == "libraryMVC.Controllers" && type.IsClass)
            .ToArray();
        Type[] services = ApplicationAssembly.GetExportedTypes()
            .Where(type => type.Namespace == "libraryMVC.Services"
                && type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters)
            .ToArray();
        Type[] applicationDependencies = controllers.Concat(services)
            .SelectMany(controller => controller.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.IsInterface && IsApplicationContract(type))
            .Distinct()
            .ToArray();

        Assert.NotEmpty(controllers);
        Assert.NotEmpty(applicationDependencies);
        Assert.All(
            applicationDependencies,
            contract => Assert.Contains(implementations, implementation =>
                ImplementsContract(implementation, contract)));
    }

    private static bool IsApplicationContract(Type type) =>
        type.Namespace is InterfacesNamespace or AbstractionsNamespace;

    private static bool ImplementsContract(Type implementation, Type contract)
    {
        if (contract.IsGenericTypeDefinition)
        {
            return implementation.GetInterfaces().Any(implemented =>
                implemented.IsGenericType && implemented.GetGenericTypeDefinition() == contract);
        }

        return contract.IsAssignableFrom(implementation);
    }
}
