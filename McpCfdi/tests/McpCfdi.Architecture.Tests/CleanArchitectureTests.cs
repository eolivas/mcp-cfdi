using NetArchTest.Rules;
using McpCfdi.Application.Interfaces;
using McpCfdi.Domain.Common;
using McpCfdi.Infrastructure.Persistence;
using Xunit;

namespace McpCfdi.Architecture.Tests;

/// <summary>
/// Architecture enforcement tests that verify Clean Architecture dependency rules
/// are respected across all layers.
/// </summary>
public class CleanArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(AggregateRoot<>).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(IApplicationEventPublisher).Assembly;

    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(McpCfdiDbContext).Assembly;

    [Fact]
    public void Domain_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("McpCfdi.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void Domain_should_not_depend_on_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("McpCfdi.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain layer must not depend on Api layer.");
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("McpCfdi.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Application layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void Application_should_not_depend_on_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("McpCfdi.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Application layer must not depend on Api layer.");
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("McpCfdi.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Infrastructure layer must not depend on Api layer.");
    }
}
