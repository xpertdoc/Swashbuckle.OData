﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Swashbuckle.Application;
using Swashbuckle.OData.Descriptions;
using Swashbuckle.Swagger;
using System.Xml.XPath;
using System.Web.Http.Dispatcher;
using System.Web.OData;

namespace Swashbuckle.OData
{
    public class ODataSwaggerDocsConfig
    {
        private readonly SwaggerDocsConfig _swaggerDocsConfig;
        private readonly List<Func<IDocumentFilter>> _documentFilters;
        private bool _includeNavigationProperties;
        internal bool enableCache;

        internal ODataSwaggerDocsConfig(SwaggerDocsConfig swaggerDocsConfig, HttpConfiguration httpConfiguration)
        {
            Contract.Requires(httpConfiguration != null);
            Contract.Requires(swaggerDocsConfig != null);

            Configuration = httpConfiguration;
            _swaggerDocsConfig = swaggerDocsConfig;
            _includeNavigationProperties = false;
            _documentFilters = new List<Func<IDocumentFilter>>();
            enableCache = false;
        }

        internal void DocumentFilter<TFilter>() where TFilter : IDocumentFilter, new()
        {
            DocumentFilter(() => new TFilter());
        }

        internal void DocumentFilter(Func<IDocumentFilter> factory)
        {
            _documentFilters.Add(factory);
        }

        internal HttpConfiguration Configuration { get; }

        internal ODataApiExplorer GetApiExplorer()
        {
            return new ODataApiExplorer(Configuration, GetODataActionDescriptorExplorers(), GetApiDescriptionMappers());
        }

        private void AddODataDocumentFilters()
        {
            if (!_includeNavigationProperties)
            {
                DocumentFilter<RemoveNavigationPropertiesFromDefinition>();
            }
        }

        private static IEnumerable<IODataActionDescriptorExplorer> GetODataActionDescriptorExplorers()
        {
            return new List<IODataActionDescriptorExplorer>
            {
                new SwaggerRouteStrategy(GetSwaggerRouteGenerators()),
                new AttributeRouteStrategy()
            };
        }

        private static IEnumerable<ISwaggerRouteGenerator> GetSwaggerRouteGenerators()
        {
            return new List<ISwaggerRouteGenerator>
            {
                new EntityDataModelRouteGenerator(),
                new CustomSwaggerRouteGenerator()
            };
        }

        private static IEnumerable<IODataActionDescriptorMapper> GetApiDescriptionMappers()
        {
            return new List<IODataActionDescriptorMapper>
            {
                new SwaggerOperationMapper(GetParameterMappers()),
                new ODataActionDescriptorMapper()
            };
        }

        private static IEnumerable<IParameterMapper> GetParameterMappers()
        {
            return new List<IParameterMapper>
            {
                new MapToODataActionParameter(),
                new MapRestierParameter(),
                new MapByParameterName(),
                new MapByDescription(),
                new MapByIndex(),
                new MapToDefault()
            };
        }

        internal SwashbuckleOptions GetSwashbuckleOptions()
        {
            AddGlobalDocumentFilters();
            AddODataDocumentFilters();

            var swaggerProviderOptions = new SwaggerProviderOptions(
                _swaggerDocsConfig.GetFieldValue<Func<ApiDescription, string, bool>>("_versionSupportResolver"),
                _swaggerDocsConfig.GetFieldValue<IEnumerable<string>>("_schemes"),
                _swaggerDocsConfig.GetSecurityDefinitions(),
                _swaggerDocsConfig.GetFieldValue<bool>("_ignoreObsoleteActions"),
                _swaggerDocsConfig.GetFieldValue<Func<ApiDescription, string>>("_groupingKeySelector"),
                _swaggerDocsConfig.GetFieldValue<IComparer<string>>("_groupingKeyComparer"),
                GetODataCustomSchemaMappings(),
                _swaggerDocsConfig.GetFieldValue<IEnumerable<Func<ISchemaFilter>>>("_schemaFilters", true).Select(factory => factory()),
                _swaggerDocsConfig.GetFieldValue<IList<Func<IModelFilter>>>("_modelFilters", true).Select(factory => factory()).ToList(),
                _swaggerDocsConfig.GetFieldValue<bool>("_ignoreObsoleteProperties"),
                _swaggerDocsConfig.GetFieldValue<Func<Type, string>>("_schemaIdSelector"),
                _swaggerDocsConfig.GetFieldValue<bool>("_describeAllEnumsAsStrings"),
                _swaggerDocsConfig.GetFieldValue<bool>("_describeStringEnumsInCamelCase"),
                GetODataOperationFilters(),
                GetODataDocumentFilters(),
                _swaggerDocsConfig.GetFieldValue<Func<IEnumerable<ApiDescription>, ApiDescription>>("_conflictingActionsResolver"),
                _swaggerDocsConfig.GetFieldValue<bool>("_applyFiltersToAllSchemas"),
                _swaggerDocsConfig.GetFieldValue<IEnumerable<Func<XPathDocument>>>("_xmlDocFactories").Select(factory=>factory).ToList()
            );

            return new SwashbuckleOptions(swaggerProviderOptions);
        }

        /// <summary>
        /// Gets custom schema mappings that will only be applied to OData operations.
        /// </summary>
        private IDictionary<Type, Func<Schema>> GetODataCustomSchemaMappings()
        {
            var customSchemaMappings = _swaggerDocsConfig.GetFieldValue<IDictionary<Type, Func<Schema>>>("_customSchemaMappings", true);
            customSchemaMappings[typeof(decimal)] = () => new Schema { type = "number", format = "decimal" };
            return customSchemaMappings;
        }

        /// <summary>
        /// Gets operation filters that will only be applied to OData operations.
        /// </summary>
        private IList<IOperationFilter> GetODataOperationFilters()
        {
            return _swaggerDocsConfig.GetFieldValue<IList<Func<IOperationFilter>>>("_operationFilters", true)
                .Select(factory => factory())
                .Concat(new EnableQueryFilter()).ToList();
        }

        /// <summary>
        /// Gets document filters that will only be applied to the SwaggerDocument built from the OData ApiExplorer.
        /// </summary>
        private IEnumerable<IDocumentFilter> GetODataDocumentFilters()
        {
            return _swaggerDocsConfig.GetFieldValue<IList<Func<IDocumentFilter>>>("_documentFilters", true)
                .Select(factory => factory())
                .Concat(_documentFilters.Select(factory => factory()));
        }

        /// <summary>
        /// Adds document filters that will be applied to SwaggerDocuments built from WebApi and OData ApiExplorers.
        /// </summary>
        private void AddGlobalDocumentFilters()
        {
            _swaggerDocsConfig.DocumentFilter<EnsureUniqueOperationIdsFilter>();
        }

        /// <summary>
        /// Gets the API versions. I'd rather not use reflection because the implementation may change, but can't find a better way.
        /// </summary>
        internal IDictionary<string, Info> GetApiVersions()
        {
            return _swaggerDocsConfig.GetFieldValue<VersionInfoBuilder>("_versionInfoBuilder", true).Build();
        }
        /// <summary>
        /// Include the navigation properties (get related entities in the models)
        /// </summary>
        public void IncludeNavigationProperties()
        {
            _includeNavigationProperties = true;
        }

        /// <summary>
        /// Enable Swagger built-in Cache
        /// </summary>
        public void EnableSwaggerRequestCaching()
        {
            enableCache = true;
        }

        /// <summary>
        /// Set a custom assembliesResolver to get the assemblies from where the types are going to be loaded
        /// </summary>
        /// <param name="assembliesResolver">custom assemblies resolver.</param>
        public void SetAssembliesResolver(IAssembliesResolver assembliesResolver)
        {
            System.Web.OData.TypeHelper.SetAssembliesResolver(assembliesResolver);
        }

        /// <summary>
        /// Set the custom Property Resolver to be used instead of using the default one.
        /// </summary>
        /// <param name="propertyResolver">Cutrom proprty resolver.</param>
        public void SetProperyResolver(IProperyResolver propertyResolver)
        {
            TypeHelper.SetProperyResolver(propertyResolver);
        }
    }
}