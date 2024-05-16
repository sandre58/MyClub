// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Domain;
using MyNet.Http;
using Newtonsoft.Json.Linq;

namespace MyClub.DataLink.Updater.Console
{
    internal abstract class WebEntityService<T> where T : Entity
    {
        protected readonly WebApiService WebApiService;
        private readonly string _request;

        protected WebEntityService(WebApiService webApiService, string request)
        {
            WebApiService = webApiService;
            _request = request;
        }

        public async Task<IEnumerable<T>> GetAsync(IDictionary<string, string> parameters)
        {
            var response = await WebApiService.GetDataAsync<object>(_request, CancellationToken.None, parameters.Select(x => (x.Key, x.Value)).ToArray()).ConfigureAwait(false);
            var result = (response as JObject)!.GetValue("response") ?? throw new InvalidOperationException(MyClubResources.GetDataFromWebError);

            return GetEntities(result, parameters);
        }

        protected abstract IEnumerable<T> GetEntities(JToken jtoken, IDictionary<string, string> parameters);
    }
}
