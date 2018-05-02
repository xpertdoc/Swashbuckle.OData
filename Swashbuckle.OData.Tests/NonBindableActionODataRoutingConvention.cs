using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData;
using System.Web.OData.Routing.Conventions;

namespace Swashbuckle.OData.Tests
{
    public class NonBindableActionODataRoutingConvention<TController> : IODataRoutingConvention
        where TController : ODataController
    {
        #region Fields

        private readonly string _controllerName;
        private readonly string[] _controllerActions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NonBindableActionODataRoutingConvention{TController}"/> class.
        /// </summary>
        public NonBindableActionODataRoutingConvention()
        {
            var controllerName = typeof(TController).Name;
            this._controllerName = controllerName.Remove(controllerName.IndexOf("Controller", StringComparison.Ordinal));
            this._controllerActions = (from m in typeof(TController).GetMethods()
                                       where m.GetCustomAttributes(typeof(HttpGetAttribute), false).Any() ||
                                             m.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()
                                       select m.Name).ToArray();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Selects the controller for OData requests.
        /// </summary>
        /// <param name="odataPath">The OData path.</param>
        /// <param name="request">The request.</param>
        /// <returns>
        /// null if the request isn't handled by this convention; otherwise, the name of the selected controller
        /// </returns>
        public string SelectController(System.Web.OData.Routing.ODataPath odataPath, HttpRequestMessage request)
        {
            if (odataPath.PathTemplate == "~/unboundaction")
            {
                var actionName = odataPath.Segments.First().Identifier;

                if (this._controllerActions.Contains(actionName))
                {
                    return this._controllerName;
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the action for OData requests.
        /// </summary>
        /// <param name="odataPath">The OData path.</param>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="actionMap">The action map.</param>
        /// <returns>
        /// null if the request isn't handled by this convention; otherwise, the name of the selected action
        /// </returns>
        public string SelectAction(System.Web.OData.Routing.ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (controllerContext.Request.Method == HttpMethod.Get || controllerContext.Request.Method == HttpMethod.Post)
            {
                if (odataPath.PathTemplate == "~/unboundaction")
                {
                    var actionName = odataPath.Segments.First().Identifier;

                    if (actionMap.Contains(actionName))
                    {
                        return actionName;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
