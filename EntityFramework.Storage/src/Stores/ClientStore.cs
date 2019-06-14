// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.EntityFramework.Stores
{
    /// <summary>
    /// Implementation of IClientStore thats uses EF.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IClientStore" />
    public class ClientStore : IClientStore
    {
        private readonly ILogger<ClientStore> _logger;
        private readonly SqlConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStore"/> class.
        /// </summary>
        /// <param name="connection">The connection to data base.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public ClientStore(SqlConnection connection, ILogger<ClientStore> logger)
        {
            _logger = logger;
            _connection = connection;
        }

        /// <summary>
        /// Finds a client by id
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>
        /// The client
        /// </returns>
        public Task<Client> FindClientByIdAsync(string clientId)
        {
            
            var client = _connection.QueryFirst<Entities.Client>("SELECT TOP (1000) *  FROM[dbo].[Clients] where ClientId = @ClientId ", new { ClientId = clientId });
           
            using (var multi = _connection.QueryMultiple(
                @"SELECT TOP (1000) * FROM [dbo].ClientGrantTypes where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientRedirectUris where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientPostLogoutRedirectUris where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientScopes where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientSecrets where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientClaims where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientIdPRestrictions where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientCorsOrigins where ClientId = @ClientId;
                  SELECT TOP (1000) * FROM [dbo].ClientProperties where ClientId = @ClientId;
                  ", new { ClientId = client.Id }))
            {

                client.AllowedGrantTypes = multi.Read<Entities.ClientGrantType>().ToList();
                client.RedirectUris = multi.Read<Entities.ClientRedirectUri>().ToList();
                client.PostLogoutRedirectUris = multi.Read<Entities.ClientPostLogoutRedirectUri>().ToList();
                client.AllowedScopes = multi.Read<Entities.ClientScope>().ToList();
                client.ClientSecrets = multi.Read<Entities.ClientSecret>().ToList();
                client.Claims = multi.Read<Entities.ClientClaim>().ToList();
                client.IdentityProviderRestrictions = multi.Read<Entities.ClientIdPRestriction>().ToList();
                client.AllowedCorsOrigins = multi.Read<Entities.ClientCorsOrigin>().ToList();
                client.Properties = multi.Read<Entities.ClientProperty>().ToList();
            }
            var model = client?.ToModel();

            _logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, model != null);

            return Task.FromResult(model);
        }
    }
}