﻿using System.Diagnostics.Contracts;
using Swashbuckle.Swagger;

namespace Swashbuckle.OData.Descriptions
{
    public class SwaggerRoute
    {
        public SwaggerRoute(string template, PathItem pathItem)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(template));
            Contract.Requires(pathItem != null);

            Template = template;
            PathItem = pathItem;
        }

        public SwaggerRoute(string template) :
            this(template, new PathItem())
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(template));
        }

        public string Template { get; }

        public PathItem PathItem { get; }
    }
}