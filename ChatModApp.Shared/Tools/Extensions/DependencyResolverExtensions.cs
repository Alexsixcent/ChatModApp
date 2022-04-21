using System.Reflection;
using DryIoc;
using ReactiveUI;

namespace ChatModApp.Shared.Tools.Extensions;

public static class DependencyResolverExtensions
{
    /// <summary>
    /// Registers inside the Splat dependency container all the classes that derive off
    /// IViewFor using Reflection. This is a easy way to register all the Views
    /// that are associated with View Models for an entire assembly.
    /// </summary>
    /// <param name="resolver">The dependency injection resolver to register the Views with.</param>
    /// <param name="assembly">The assembly to search using reflection for IViewFor classes.</param>
    /// <param name="parentNamespace">The parent namespace of the views.</param>
    public static void RegisterViewsForViewModels(this IContainer container, Assembly assembly,
                                                  string parentNamespace = "")
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container));

        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));


        var typeInfos = assembly.DefinedTypes
                                .Where(ti =>
                                           ti.Namespace is not null
                                           && ti.Namespace.StartsWith(parentNamespace)
                                           && !ti.Name.EndsWith("Base")
                                           && ti.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IViewFor))
                                           && !ti.IsAbstract);


        // for each type that implements IViewFor
        foreach (var ti in typeInfos)
        {
            // grab the first _implemented_ interface that also implements IViewFor, this should be the expected IViewFor<>
            var ivf = ti.ImplementedInterfaces.FirstOrDefault(t =>
                                                                  t.GetTypeInfo().ImplementedInterfaces
                                                                   .Contains(typeof(IViewFor)));

            // need to check for null because some classes may implement IViewFor but not IViewFor<T> - we don't care about those
            if (ivf is null)
                continue;

            // my kingdom for c# 6!
            var contractSource = ti.GetCustomAttribute<ViewContractAttribute>();
            var contract = contractSource is not null ? contractSource.Contract : string.Empty;

            RegisterType(container, ti, ivf, contract);
        }
    }

    private static void RegisterType(IContainer container, TypeInfo ti, Type serviceType,
                                     string contract)
    {
        var constructor = GetConstructor(ti);
            
        container.Register(serviceType, ti.UnderlyingSystemType,
                           ti.GetCustomAttribute<SingleInstanceViewAttribute>() is not null
                               ? Reuse.Singleton
                               : Reuse.Transient, Made.Of(constructor));
    }

    private static ConstructorInfo GetConstructor(TypeInfo typeInfo)
    {
        var parameterlessConstructor =
            typeInfo.DeclaredConstructors.FirstOrDefault(ci => ci.IsPublic && !ci.GetParameters().Any());
        if (parameterlessConstructor is null)
        {
            throw new(
                      $"Failed to register type {typeInfo.FullName} because it's missing a parameter-less constructor.");
        }

        return parameterlessConstructor;
    }
}