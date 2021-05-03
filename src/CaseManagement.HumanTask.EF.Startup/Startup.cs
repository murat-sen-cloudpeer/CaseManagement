﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using CaseManagement.HumanTask.AspNetCore;
using CaseManagement.HumanTask.Builders;
using CaseManagement.HumanTask.Domains;
using CaseManagement.HumanTask.Persistence.EF;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace CaseManagement.HumanTask.EF.Startup
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        private readonly IConfiguration _configuration;

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            services.AddMvc(opts => opts.EnableEndpointRouting = false).AddNewtonsoftJson();
            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = ExtractKey("openid_puk.txt"),
                    ValidAudiences = new List<string>
                    {
                        "http://localhost:60000",
                        "https://simpleidserver.northeurope.cloudapp.azure.com/openid"
                    },
                    ValidIssuers = new List<string>
                    {
                        "http://localhost:60000",
                        "https://simpleidserver.northeurope.cloudapp.azure.com/openid"
                    }
                };
            })
            .AddJwtBearer("OAuthScheme", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = ExtractKey("oauth_puk.txt"),
                    ValidAudiences = new List<string>
                    {
                        "bpmnClient",
                        "cmmnClient"
                    },
                    ValidIssuers = new List<string>
                    {
                        "http://localhost:60001",
                        "https://simpleidserver.northeurope.cloudapp.azure.com/oauth"
                    }
                };
            }); ;
            services.AddAuthorization(_ => _.AddDefaultHumanTaskAuthorizationPolicy());
            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));
            services.AddHumanTasksApi();
            services.AddHumanTaskServer();
            services.AddHumanTaskStoreEF(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("db"), o => o.MigrationsAssembly(migrationsAssembly));
            });
            services.AddSwaggerGen();
            services.AddHostedService<HumanTaskJobServerHostedService>();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            InitializeDatabase(app);
            app.UseCulture();
            app.UseForwardedHeaders();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var edp = "/swagger/v1/swagger.json";
                c.SwaggerEndpoint(edp, "WS-HumanTask API V1");
            });
            app.UseAuthentication();
            app.UseCors("AllowAll");
            app.UseMvc();
        }

        private RsaSecurityKey ExtractKey(string fileName)
        {
            var json = File.ReadAllText(Path.Combine(_env.ContentRootPath, fileName));
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var rsa = RSA.Create();
            var rsaParameters = new RSAParameters
            {
                Modulus = Base64DecodeBytes(dic["n"].ToString()),
                Exponent = Base64DecodeBytes(dic["e"].ToString())
            };
            rsa.ImportParameters(rsaParameters);
            return new RsaSecurityKey(rsa);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = scope.ServiceProvider.GetService<HumanTaskDBContext>())
                {
                    context.Database.Migrate();
                    if (context.HumanTaskDefinitions.Any())
                    {
                        return;
                    }

                    foreach(var humanTaskDef in GetHumanTaskDefs())
                    {
                        context.HumanTaskDefinitions.Add(humanTaskDef);
                    }

                    context.SaveChanges();
                }
            }
        }

        private static List<HumanTaskDefinitionAggregate> GetHumanTaskDefs()
        {
            var dressAppropriateForm = HumanTaskDefBuilder.New("dressAppropriateForm")
                .SetTaskInitiatorUserIdentifiers(new List<string> { "businessanalyst" })
                .SetPotentialOwnerUserIdentifiers(new List<string> { "businessanalyst" })
                .AddPresentationParameter("degree", ParameterTypes.STRING, "context.GetInput(\"degree\")")
                .AddPresentationParameter("city", ParameterTypes.STRING, "context.GetInput(\"city\")")
                .AddName("fr", "Température à $city$")
                .AddName("en", "Temperature in $city$")
                .AddSubject("fr", "Il fait $degree$", "text/html")
                .AddSubject("en", "Degree : $degree$", "text/html")
                .AddInputOperationParameter("degree", ParameterTypes.STRING, true)
                .AddInputOperationParameter("city", ParameterTypes.STRING, true)
                .Build();
            var captureClaimDetails = HumanTaskDefBuilder.New("captureClaimDetails")
                .SetTaskInitiatorUserIdentifiers(new List<string> { "businessanalyst" })
                .SetPotentialOwnerUserIdentifiers(new List<string> { "businessanalyst" })
                .AddName("fr", "Capturer les détails de la réclamation")
                .AddName("en", "Capture claim details")
                .SetRendering("{'type':'container','children':[{'id':'d2a121d7-fe66-4cb3-a7fb-b9f48219ab228','type':'txt','label':'Firstname','name':'firstName'},{'id':'c6a4ca0f-aa78-4743-9787-e5126375945bd','type':'txt','label':'Lastname','name':'lastName'},{'id':'20b21433-b934-4095-b942-06eb73a15b14e','type':'txt','label':'Claim','name':'claim'}]}")
                .AddOutputOperationParameter("firstName", ParameterTypes.STRING, true)
                .AddOutputOperationParameter("lastName", ParameterTypes.STRING, true)
                .AddOutputOperationParameter("claim", ParameterTypes.STRING, true)
                .Build();
            var takeTemperatureForm = HumanTaskDefBuilder.New("temperatureForm")
                .SetTaskInitiatorUserIdentifiers(new List<string> { "businessanalyst" })
                .SetPotentialOwnerUserIdentifiers(new List<string> { "businessanalyst" })
                .AddName("fr", "Saisir la température")
                .AddName("en", "Enter degree")
                .SetRendering("{'type':'container','children':[{'id':'ea71ffe8-517f-4f52-97f0-2658ee0bb1c92','type':'txt','label':'Degree','name':'degree'}]}")
                .AddOutputOperationParameter("degree", ParameterTypes.INT, true)
                .Build();
            var updateClaimantContactDetailsForm = HumanTaskDefBuilder.New("updateClaimantContactDetailsForm")
                .SetTaskInitiatorUserIdentifiers(new List<string> { "businessanalyst" })
                .SetPotentialOwnerUserIdentifiers(new List<string> { "businessanalyst" })
                .AddName("fr", "Mettre à jour les informations de contact du 'Claimant'")
                .AddName("en", "Update claimant contact details")
                .SetRendering("{'type':'container','children':[{'id':'c6d8ca40-eb7a-48f4-8849-26afc0ffb4cda','type':'txt','label':'Firstname','name':'firstName'},{'id':'3e163681-2147-40f5-9c77-391ad5699c905','type':'txt','label':'Lastname','name':'lastName'}]}")
                .AddOutputOperationParameter("firstName", ParameterTypes.STRING, true)
                .AddOutputOperationParameter("lastName", ParameterTypes.STRING, true)
                .Build();
            return new List<HumanTaskDefinitionAggregate>
            {
                dressAppropriateForm,
                captureClaimDetails,
                takeTemperatureForm,
                updateClaimantContactDetailsForm
            };
        }

        private static byte[] Base64DecodeBytes(string base64EncodedData)
        {
            var s = base64EncodedData
                .Trim()
                .Replace(" ", "+")
                .Replace('-', '+')
                .Replace('_', '/');
            switch (s.Length % 4)
            {
                case 0:
                    return Convert.FromBase64String(s);
                case 2:
                    s += "==";
                    goto case 0;
                case 3:
                    s += "=";
                    goto case 0;
                default:
                    throw new InvalidOperationException("Illegal base64url string!");
            }
        }
    }
}