﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;
using Owin;
using Swashbuckle.Swagger;
using SwashbuckleODataSample.Models;

namespace Swashbuckle.OData.Tests
{
    [TestFixture]
    public class ActionTests
    {
        [Test]
        public async Task It_supports_actions_that_accept_an_array_of_complex_types()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(SuppliersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route and post data to the test controller is valid
                var suppliers = new SupplierDtos
                {
                    Suppliers = new List<SupplierDto>
                    {
                        new SupplierDto
                        {
                            Name = "SupplierNameOne",
                            Code = "CodeOne",
                            Description = "SupplierOneDescription"
                        },
                        new SupplierDto
                        {
                            Name = "SupplierNameTwo",
                            Code = "CodeTwo",
                            Description = "SupplierTwoDescription"
                        }
                    }
                };

                var result = await httpClient.PostAsJsonAsync("/odata/Suppliers/Default.PostArrayOfSuppliers", suppliers);
                result.IsSuccessStatusCode.Should().BeTrue();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Suppliers/Default.PostArrayOfSuppliers", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.post.Should().NotBeNull();
                pathItem.post.parameters.Count.Should().Be(1);
                pathItem.post.parameters.Single().@in.Should().Be("body");
                pathItem.post.parameters.Single().name.Should().Be("parameters");
                pathItem.post.parameters.Single().schema.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.type.Should().Be("object");
                pathItem.post.parameters.Single().schema.properties.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Count.Should().Be(1);
                pathItem.post.parameters.Single().schema.properties.Should().ContainKey("suppliers");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "suppliers").Value.type.Should().Be("array");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "suppliers").Value.items.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "suppliers").Value.items.@ref.Should().Be("#/definitions/SupplierDto");

                swaggerDocument.definitions.Keys.Should().Contain("SupplierDto");

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        [Test]
        public async Task It_supports_actions_with_only_body_paramters()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(SuppliersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var supplierDto = new SupplierDto
                {
                    Name = "SupplierName",
                    Code = "SDTO",
                    Description = "SupplierDescription"
                };
                var result = await httpClient.PostAsJsonAsync("/odata/Suppliers/Default.Create", supplierDto);
                result.IsSuccessStatusCode.Should().BeTrue();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Suppliers/Default.Create", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.post.Should().NotBeNull();
                pathItem.post.parameters.Count.Should().Be(1);
                pathItem.post.parameters.Single().@in.Should().Be("body");
                pathItem.post.parameters.Single().schema.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Count.Should().Be(3);
                pathItem.post.parameters.Single().schema.properties.Should().ContainKey("code");
                pathItem.post.parameters.Single().schema.properties.Should().ContainKey("name");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "name").Value.type.Should().Be("string");
                pathItem.post.parameters.Single().schema.properties.Should().ContainKey("description");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "description").Value.type.Should().Be("string");
                pathItem.post.parameters.Single().schema.required.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.required.Count.Should().Be(2);
                pathItem.post.parameters.Single().schema.required.Should().Contain("code");
                pathItem.post.parameters.Single().schema.required.Should().Contain("name");

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        [Test]
        public async Task It_supports_actions_with_an_optional_enum_parameter()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(SuppliersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var supplierDto = new SupplierWithEnumDto
                {
                    EnumValue = MyEnum.ValueOne
                };
                var result = await httpClient.PostAsJsonAsync("/odata/Suppliers/Default.CreateWithEnum", supplierDto);
                result.IsSuccessStatusCode.Should().BeTrue();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Suppliers/Default.CreateWithEnum", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.post.Should().NotBeNull();
                pathItem.post.parameters.Count.Should().Be(1);
                pathItem.post.parameters.Single().@in.Should().Be("body");
                pathItem.post.parameters.Single().schema.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.type.Should().Be("object");
                pathItem.post.parameters.Single().schema.properties.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Count.Should().Be(1);
                pathItem.post.parameters.Single().schema.properties.Should().ContainKey("EnumValue");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "EnumValue").Value.type.Should().Be("string");
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "EnumValue").Value.@enum.Should().NotBeNull();
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "EnumValue").Value.@enum.Count.Should().Be(2);
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "EnumValue").Value.@enum.First().Should().Be(MyEnum.ValueOne.ToString());
                pathItem.post.parameters.Single().schema.properties.Single(pair => pair.Key == "EnumValue").Value.@enum.Skip(1).First().Should().Be(MyEnum.ValueTwo.ToString());
                pathItem.post.parameters.Single().schema.required.Should().BeNull();

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        [Test]
        public async Task It_supports_actions_against_an_entity()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(SuppliersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var rating = new RatingDto
                {
                    Rating = 1
                };
                var result = await httpClient.PostAsJsonAsync("/odata/Suppliers(1)/Default.Rate", rating);
                result.IsSuccessStatusCode.Should().BeTrue();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Suppliers({Id})/Default.Rate", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.post.Should().NotBeNull();
                pathItem.post.parameters.Count.Should().Be(2);

                var idParameter = pathItem.post.parameters.SingleOrDefault(parameter => parameter.@in == "path");
                idParameter.Should().NotBeNull();
                idParameter.type.Should().Be("integer");
                idParameter.format.Should().Be("int32");
                idParameter.name.Should().Be("Id");

                var bodyParameter = pathItem.post.parameters.SingleOrDefault(parameter => parameter.@in == "body");
                bodyParameter.Should().NotBeNull();
                bodyParameter.@in.Should().Be("body");
                bodyParameter.schema.Should().NotBeNull();
                bodyParameter.schema.type.Should().Be("object");
                bodyParameter.schema.properties.Should().NotBeNull();
                bodyParameter.schema.properties.Count.Should().Be(1);
                bodyParameter.schema.properties.Should().ContainKey("Rating");
                bodyParameter.schema.properties.Single(pair => pair.Key == "Rating").Value.type.Should().Be("integer");
                bodyParameter.schema.properties.Single(pair => pair.Key == "Rating").Value.format.Should().Be("int32");
                bodyParameter.schema.required.Should().NotBeNull();
                bodyParameter.schema.required.Count.Should().Be(1);
                bodyParameter.schema.required.Should().Contain("Rating");

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        [Test]
        public async Task It_supports_unbound_actions()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(SuppliersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var rating = new RatingDto
                {
                    Rating = 1
                };
                var result = await httpClient.PostAsJsonAsync("/odata/Suppliers(1)/Default.Rate", rating);
                result.IsSuccessStatusCode.Should().BeTrue();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Calculate", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.post.Should().NotBeNull();
                pathItem.post.parameters.Count.Should().Be(1);

                var bodyParameter = pathItem.post.parameters.SingleOrDefault(parameter => parameter.@in == "body");
                bodyParameter.Should().NotBeNull();
                bodyParameter.@in.Should().Be("body");
                bodyParameter.schema.Should().NotBeNull();
                bodyParameter.schema.type.Should().Be("object");
                bodyParameter.schema.properties.Should().NotBeNull();
                bodyParameter.schema.properties.Count.Should().Be(2);
                bodyParameter.schema.properties.Should().ContainKey("amount");
                bodyParameter.schema.properties.Single(pair => pair.Key == "amount").Value.type.Should().Be("integer");
                bodyParameter.schema.properties.Single(pair => pair.Key == "amount").Value.format.Should().Be("int32");
                bodyParameter.schema.properties.Should().ContainKey("label");
                bodyParameter.schema.properties.Single(pair => pair.Key == "label").Value.type.Should().Be("string");
                bodyParameter.schema.properties.Single(pair => pair.Key == "label").Value.format.Should().BeNull();
                bodyParameter.schema.required.Should().NotBeNull();
                bodyParameter.schema.required.Count.Should().Be(1);

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        private static void Configuration(IAppBuilder appBuilder, Type targetController)
        {
            var config = appBuilder.GetStandardHttpConfig(targetController);

            var routingConventions = ODataRoutingConventions.CreateDefault();
            routingConventions.Insert(routingConventions.Count - 1, new NonBindableActionODataRoutingConvention<SuppliersController>());

            // Define a route to a controller class that contains functions
            config.MapODataServiceRoute("ODataRoute", "odata", GetEdmModel(), new DefaultODataPathHandler(), routingConventions);

            config.EnsureInitialized();
        }

        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Supplier>("Suppliers");
            //builder.ComplexType<SupplierDto>();
            var entityType = builder.EntityType<Supplier>();

            var create = entityType.Collection.Action("Create");
            create.ReturnsFromEntitySet<Supplier>("Suppliers");
            create.Parameter<string>("code").OptionalParameter = false;
            create.Parameter<string>("name").OptionalParameter = false;
            create.Parameter<string>("description");

            var createWithEnum = entityType.Collection.Action("CreateWithEnum");
            createWithEnum.ReturnsFromEntitySet<Supplier>("Suppliers");
            createWithEnum.Parameter<MyEnum?>("EnumValue");

            var postArray = entityType.Collection.Action("PostArrayOfSuppliers");
            postArray.ReturnsCollectionFromEntitySet<Supplier>("Suppliers");
            postArray.CollectionParameter<SupplierDto>("suppliers");

            var unbound = builder.Action("Calculate");
            unbound.Parameter<int>("amount");
            unbound.Parameter<string>("label");

            entityType.Action("Rate")
                .Parameter<int>("Rating");

            return builder.GetEdmModel();
        }
    }

    public class SupplierDtos
    {
        [JsonProperty("suppliers")]
        public List<SupplierDto> Suppliers { get; set; }
    }

    public class SupplierDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SupplierWithEnumDto
    {
        public MyEnum EnumValue { get; set; }
    }

    public class RatingDto
    {
        public int Rating { get; set; }
    }

    public class Supplier
    {
        [Key]
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SuppliersController : ODataController
    {
        [HttpPost]
        [ResponseType(typeof(List<Supplier>))]
        public List<Supplier> PostArrayOfSuppliers(ODataActionParameters parameters)
        {
            parameters.Should().ContainKey("suppliers");
            parameters.Count.Should().Be(1);
            parameters["suppliers"].Should().BeAssignableTo<IEnumerable<SupplierDto>>();

            return new List<Supplier>();
        }

        [HttpPost]
        [ResponseType(typeof(Supplier))]
        public IHttpActionResult Create(ODataActionParameters parameters)
        {
            return Created(new Supplier {Id = 1});
        }

        [HttpPost]
        [ResponseType(typeof(Supplier))]
        public IHttpActionResult CreateWithEnum(ODataActionParameters parameters)
        {
            return Created(new Supplier { Id = 1 });
        }

        [HttpPost]
        [ResponseType(typeof(Supplier))]
        public IHttpActionResult Calculate(ODataActionParameters parameters)
        {
            return Created(new Supplier { Id = 5 });
        }

        [HttpPost]
        public IHttpActionResult Rate([FromODataUri] int key, ODataActionParameters parameters)
        {
            parameters.Should().ContainKey("Rating");

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}