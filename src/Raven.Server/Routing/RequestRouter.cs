// -----------------------------------------------------------------------
//  <copyright file="RequestRouter.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Raven.Server.ServerWide;
using Raven.Server.Web;

namespace Raven.Server.Routing
{
    public class RequestRouter
    {
        private readonly Trie<RouteInformation> _trie;
        private ServerStore _serverStore;

        public RequestRouter(Dictionary<string, RouteInformation> routes)
        {
            _trie = Trie<RouteInformation>.Build(routes);
        }

        public async Task HandlePath(HttpContext context)
        {
            //TODO: Kestrel bug https://github.com/aspnet/KestrelHttpServer/issues/617
            //TODO: requires us to do this

            var method = context.Request.Method.Trim();

            var tryMatch = _trie.TryMatch(method, context.Request.Path.Value);
            if (tryMatch.Value == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"There is no handler for path: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                return;
            }

            if(_serverStore == null)// okay that this is not thread safe, end up with same value
                _serverStore = (ServerStore)context.RequestServices.GetService(typeof(ServerStore));

            var reqCtx = new RequestHandlerContext
            {
                HttpContext = context,
                ServerStore = _serverStore,
                RouteMatch = tryMatch.Match,
            };

            var handler = await tryMatch.Value.CreateHandler(reqCtx);
            if (handler == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("There is no handler for {context.Request.Method} {context.Request.Path}");
                return;
            }

            await handler(reqCtx);
        }
    }
}