﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http.Dispatcher;
using System.Web.OData.Formatter;

namespace System.Web.OData
{
    /// <summary> 
    /// Provides an abstraction for managing the properties of an type.
    /// </summary>
    public interface IProperyResolver
    {
        string ResolveName(MemberInfo memberInfo, string defaultName);
    }

    /// <summary>
    /// Default proprty resolver.
    /// </summary>
    /// <remarks>Resolve the property name from DataMemberAttribute.</remarks>
    public class DefaultProperyResolver : IProperyResolver
    {
        public string ResolveName(MemberInfo memberInfo, string defaultName)
        {
            Contract.Requires(memberInfo != null);

            var dataMemberAttribute = memberInfo.GetCustomAttributes<DataMemberAttribute>()?.SingleOrDefault();
            return !string.IsNullOrWhiteSpace(dataMemberAttribute?.Name) ? dataMemberAttribute.Name : defaultName;
        }
    }

    internal static class TypeHelper
    {
        private static IAssembliesResolver _assembliesResolver =  new DefaultAssembliesResolver();
        private static IProperyResolver _propertyResolver = new DefaultProperyResolver();

        internal static Type ToNullable(this Type t)
        {
            Contract.Requires(t != null);

            if (t.IsNullable())
            {
                return t;
            }
            return typeof (Nullable<>).MakeGenericType(t);
        }

        // Gets the collection element type.
        internal static Type GetInnerElementType(this Type type)
        {
            Contract.Requires(type != null);

            Type elementType;
            type.IsCollection(out elementType);
            Contract.Assert(elementType != null);

            return elementType;
        }

        internal static bool IsCollection(this Type type)
        {
            if (type == null)
                return false;
            Type elementType;
            return type.IsCollection(out elementType);
        }

        internal static bool IsCollection(this Type type, out Type elementType)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.ValueAtReturn(out elementType) != null);

            elementType = type;

            // see if this type should be ignored.
            if (type == typeof (string))
            {
                return false;
            }

            var collectionInterface = type.GetInterfaces().Union(new[]
            {
                type
            }).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IEnumerable<>));

            if (collectionInterface != null)
            {
                elementType = collectionInterface.GetGenericArguments().Single();

                Contract.Assume(elementType != null);
                return true;
            }

            return false;
        }

        internal static Type GetUnderlyingTypeOrSelf(Type type)
        {
            Contract.Requires(type != null);

            return Nullable.GetUnderlyingType(type) ?? type;
        }

        internal static bool IsEnum(Type type)
        {
            Contract.Requires(type != null);

            var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
            return underlyingTypeOrSelf.IsEnum;
        }

        /// <summary>
        /// Set the custom Assemblies Resolver to be used instead of using the default one
        /// </summary>
        /// <param name="assembliesResolver"></param>
        internal static void SetAssembliesResolver(IAssembliesResolver assembliesResolver)
        {
            _assembliesResolver = assembliesResolver;
        }

        /// <summary>
        /// Set the custom Property Resolver to be used instead of using the default one.
        /// </summary>
        /// <param name="propertyResolver">Property Resolver.</param>
        /// <remarks>Change default property name strategy (name from DataMemberAttribute) to custom one.</remarks>
        internal static void SetProperyResolver(IProperyResolver propertyResolver)
        {
            _propertyResolver = propertyResolver;
        }

        /// <summary>
        ///     Determines whether the given type is a primitive type or
        ///     is a <see cref="string" />, <see cref="DateTime" />, <see cref="Decimal" />,
        ///     <see cref="Guid" />, <see cref="DateTimeOffset" /> or <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns><c>true</c> if the type is a primitive type.</returns>
        internal static bool IsQueryPrimitiveType(Type type)
        {
            Contract.Requires(type != null);

            type = GetInnerMostElementType(type);

            return type.IsEnum || type.IsPrimitive || type == typeof (Uri) || (EdmLibHelpers.GetEdmPrimitiveTypeOrNull(type) != null);
        }

        /// <summary>
        ///     Returns the innermost element type for a given type, dealing with
        ///     nullables, arrays, etc.
        /// </summary>
        /// <param name="type">The type from which to get the innermost type.</param>
        /// <returns>The innermost element type</returns>
        internal static Type GetInnerMostElementType(Type type)
        {
            Contract.Requires(type != null);

            while (true)
            {
                var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
                if (nullableUnderlyingType != null)
                {
                    type = nullableUnderlyingType;
                }
                else if (type.HasElementType)
                {
                    type = type.GetElementType();
                    Contract.Assume(type != null);
                }
                else
                {
                    return type;
                }
            }
        }

        /// <summary>
        ///     Returns type of T if the type implements IEnumerable of T, otherwise, return null.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Type GetImplementedIEnumerableType(Type type)
        {
            Contract.Requires(type != null);

            // get inner type from Task<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Task<>))
            {
                var genericArguments = type.GetGenericArguments();
                Contract.Assume(genericArguments.Any());
                type = genericArguments.First();
            }
            Contract.Assume(type != null);
            if (type.IsGenericType && type.IsInterface && (type.GetGenericTypeDefinition() == typeof (IEnumerable<>) || type.GetGenericTypeDefinition() == typeof (IQueryable<>)))
            {
                // special case the IEnumerable<T>
                return GetInnerGenericType(type);
            }
            // for the rest of interfaces and strongly Type collections
            var interfaces = type.GetInterfaces();
            return (from interfaceType in interfaces
                where interfaceType.IsGenericType && (interfaceType.GetGenericTypeDefinition() == typeof (IEnumerable<>) || interfaceType.GetGenericTypeDefinition() == typeof (IQueryable<>))
                select GetInnerGenericType(interfaceType)).FirstOrDefault();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching all exceptions in this case is the right to do.")]
        // This code is copied from DefaultHttpControllerTypeResolver.GetControllerTypes.
        internal static IEnumerable<Type> GetLoadedTypes()
        {
            var result = new List<Type>();

            // Go through all assemblies referenced by the application and search for types matching a predicate
            var assemblies = _assembliesResolver.GetAssemblies();
            Contract.Assume(assemblies != null);
            foreach (var assembly in assemblies)
            {
                Type[] exportedTypes;
                if (assembly == null || assembly.IsDynamic)
                {
                    // can't call GetTypes on a null (or dynamic?) assembly
                    continue;
                }

                try
                {
                    exportedTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    exportedTypes = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (exportedTypes != null)
                {
                    result.AddRange(exportedTypes.Where(t => t != null && t.IsVisible));
                }
            }

            return result;
        }

        /// <summary>
        /// Resolve a proprty name from the metadata.
        /// </summary>
        /// <param name="member">Member Info.</param>
        /// <param name="defaultName">Default property name.</param>
        /// <returns>Proprty name.</returns>
        internal static string GetPropertyName(MemberInfo member, string defaultName)
        {
            return _propertyResolver.ResolveName(member, defaultName);
        }
        /// <summary>
        /// Resolve a proprty name from the metadata.
        /// </summary>
        /// <param name="member">Member Info.</param>
        /// <returns>Proprty name.</returns>
        internal static string GetPropertyName(MemberInfo member)
        {
            return _propertyResolver.ResolveName(member, member.Name);
        }

        private static Type GetInnerGenericType(Type interfaceType)
        {
            Contract.Requires(interfaceType != null);
            Contract.Requires(interfaceType.GetGenericArguments() != null);

            // Getting the type T definition if the returning type implements IEnumerable<T>
            var parameterTypes = interfaceType.GetGenericArguments();

            if (parameterTypes.Length == 1)
            {
                return parameterTypes[0];
            }

            return null;
        }

        /// <summary>
        /// Looks in all loaded assemblies for the given type.
        /// </summary>
        /// <param name="fullName">
        /// The full name of the type.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/> found; null if not found.
        /// </returns>
        internal static Type FindType(string fullName)
        {
            return GetLoadedTypes()
                .First(t => t.FullName.Equals(fullName));
        }
    }
}