namespace MazyPlatform.Service.Gateway.Configuration;

using System.Text;
using System.Threading.RateLimiting;

using Grpc.Net.ClientFactory;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Common.Extensions;
using MazyPlatform.Service.Gateway.Interceptors;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

using Serilog;

internal static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddConfigure()
        {
            builder.AddSerilog();
            builder.AddGrpc();
            builder.AddGrpcClients();
            builder.AddAuthentication();
            builder.AddHealthChecks();
            builder.AddForwardedHeaders();
            builder.AddRateLimiting();

            return builder;
        }

        private void AddSerilog()
            => builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

        private void AddGrpc()
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()!;

            builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    p.AllowAnyOrigin();
                    p.AllowAnyMethod();
                    p.AllowAnyHeader();
                    p.WithExposedHeaders(TraceContext.HeaderName);
                }
                else
                {
                    p.WithOrigins(origins);
                    p.AllowAnyMethod();
                    p.AllowAnyHeader();
                    p.WithExposedHeaders(TraceContext.HeaderName);
                }
            }));

            builder.Services.AddGrpc(o =>
            {
                o.Interceptors.Add<TraceIdInterceptor>();
                o.Interceptors.Add<LoggingInterceptor>();
            }).AddJsonTranscoding(o =>
            {
                o.TypeRegistry = Google.Protobuf.Reflection.TypeRegistry.FromFiles(
                    Google.Rpc.ErrorDetailsReflection.Descriptor,
                    Google.Rpc.StatusReflection.Descriptor);
            });
            builder.Services.AddGrpcSwagger();
            builder.Services.AddSwaggerGen();
        }

        private void AddGrpcClients()
        {
            var options = builder.Configuration
                .GetSection(ServicesOptions.SectionName)
                .Get<ServicesOptions>()!;

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<HeadersPropagationInterceptor>();

            builder.AddAuthenticationGrpcClients(new Uri(options.Authentication));
            builder.AddScenarioRepositoryGrpcClients(new Uri(options.ScenarioRepository));
            builder.AddBotManagerGrpcClients(new Uri(options.BotManager));
        }

        private void AddAuthentication()
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
                    };
                    o.MapInboundClaims = false;
                });

            builder.Services.AddAuthorization();
        }

        private void AddForwardedHeaders()
        {
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
                    | ForwardedHeaders.XForwardedHost;
                o.KnownIPNetworks.Clear();
                o.KnownProxies.Clear();
            });
        }

        private void AddHealthChecks()
        {
            builder.Services
                .AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
                .AddCheck<GatewayConfigurationHealthCheck>("configuration", tags: ["ready"]);
        }

        private void AddRateLimiting()
        {
            builder.Services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy(GatewayRateLimitPolicies.PublicAuth, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientPartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));
            });
        }

        private static string GetClientPartitionKey(HttpContext context)
            => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private void AddAuthenticationGrpcClients(Uri address)
        {
            builder.Services.AddGrpcClient<AuthenticationService.AuthenticationServiceClient>(Configure);
            builder.Services.AddGrpcClient<MfaService.MfaServiceClient>(Configure);
            builder.Services.AddGrpcClient<MfaSessionService.MfaSessionServiceClient>(Configure);
            builder.Services.AddGrpcClient<PasswordService.PasswordServiceClient>(Configure);
            builder.Services.AddGrpcClient<RegistrationService.RegistrationServiceClient>(Configure);
            builder.Services.AddGrpcClient<UserSessionService.UserSessionServiceClient>(Configure);
            builder.Services.AddGrpcClient<LinkedProviderService.LinkedProviderServiceClient>(Configure);

            void Configure(GrpcClientFactoryOptions o)
            {
                o.Address = address;
                o.AddHeadersPropagation();
            }
        }

        private void AddScenarioRepositoryGrpcClients(Uri address)
        {
            builder.Services.AddGrpcClient<ProjectService.ProjectServiceClient>(Configure);
            builder.Services.AddGrpcClient<EntitySchemaService.EntitySchemaServiceClient>(Configure);
            builder.Services.AddGrpcClient<ScenarioGraphService.ScenarioGraphServiceClient>(Configure);
            builder.Services.AddGrpcClient<UserDataService.UserDataServiceClient>(Configure);

            void Configure(GrpcClientFactoryOptions o)
            {
                o.Address = address;
                o.AddHeadersPropagation();
            }
        }

        private void AddBotManagerGrpcClients(Uri address)
        {
            builder.Services.AddGrpcClient<BotService.BotServiceClient>(Configure);

            void Configure(GrpcClientFactoryOptions o)
            {
                o.Address = address;
                o.AddHeadersPropagation();
            }
        }
    }
}
