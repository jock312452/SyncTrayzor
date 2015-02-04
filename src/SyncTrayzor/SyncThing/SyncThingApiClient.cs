﻿using Newtonsoft.Json;
using Refit;
using SyncTrayzor.SyncThing.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingApiClient
    {
        void SetConnectionDetails(Uri baseAddress, string apiKey);

        Task ShutdownAsync();
        Task<List<Event>> FetchEventsAsync(int since, int? limit = null);
        Task<Config> FetchConfigAsync();
    }

    public class SyncThingApiClient : ISyncThingApiClient
    {
        private ISyncThingApi api;

        public void SetConnectionDetails(Uri baseAddress, string apiKey)
        {
            var httpClient = new HttpClient(new AuthenticatedHttpClientHandler(apiKey))
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(70),
            };
            this.api = RestService.For<ISyncThingApi>(httpClient, new RefitSettings()
            {
                JsonSerializerSettings = new JsonSerializerSettings()
                {
                    Converters = { new EventConverter() }
                },
            });
        }

        public Task ShutdownAsync()
        {
            this.EnsureSetup();
            return this.api.ShutdownAsync();
        }

        public Task<List<Event>> FetchEventsAsync(int since, int? limit)
        {
            this.EnsureSetup();
            if (limit == null)
                return this.api.FetchEventsAsync(since);
            else
                return this.api.FetchEventsLimitAsync(since, limit.Value);
        }

        public Task<Config> FetchConfigAsync()
        {
            this.EnsureSetup();
            return this.api.FetchConfigAsync();
        }

        private void EnsureSetup()
        {
            if (this.api == null)
                throw new InvalidOperationException("SetConnectionDetails not called");
        }

        private class AuthenticatedHttpClientHandler : HttpClientHandler
        {
            private readonly string apiKey;

            public AuthenticatedHttpClientHandler(string apiKey)
            {
                this.apiKey = apiKey;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Headers.Add("X-API-Key", this.apiKey);
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
