using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ReactiveUI;
using Splat;

namespace ChatModApp.Tools.Extensions
{
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
        public static void RegisterViewsForViewModels(this IMutableDependencyResolver resolver, Assembly assembly,
                                                      string parentNamespace = "")
        {
            if (resolver is null)
                throw new ArgumentNullException(nameof(resolver));

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

                RegisterType(resolver, ti, ivf, contract);
            }
        }

        private static void RegisterType(IMutableDependencyResolver resolver, TypeInfo ti, Type serviceType,
                                         string contract)
        {
            var factory = TypeFactory(ti);
            if (ti.GetCustomAttribute<SingleInstanceViewAttribute>() is not null)
            {
                resolver.RegisterLazySingleton(factory, serviceType, contract);
            }
            else
            {
                resolver.Register(factory, serviceType, contract);
            }
        }

        private static Func<object> TypeFactory(TypeInfo typeInfo)
        {
            var parameterlessConstructor =
                typeInfo.DeclaredConstructors.FirstOrDefault(ci => ci.IsPublic && !ci.GetParameters().Any());
            if (parameterlessConstructor is null)
            {
                throw new Exception(
                    $"Failed to register type {typeInfo.FullName} because it's missing a parameter-less constructor.");
            }

            return Expression.Lambda<Func<object>>(Expression.New(parameterlessConstructor)).Compile();
        }
    }
}