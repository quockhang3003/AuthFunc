using System.Security.Claims;
using System.Text;
using DataAccess.Implements;
using DataAccess.Interfaces;
using Domain.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service.Implements;
using Service.Interfaces;

namespace API.Extensions;

public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecret = configuration["JwtSettings:Secret"];
            var jwtIssuer = configuration["JwtSettings:Issuer"];
            var jwtAudience = configuration["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(jwtSecret))
                throw new InvalidOperationException("JWT Secret is not configured");

            var key = Encoding.UTF8.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var response = new { message = "Authentication required", success = false };
                        return context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var response = new { message = "Access denied", success = false };
                        return context.Response.WriteAsJsonAsync(response);
                    }
                };
            })
            .AddNegotiate(); // Add Windows Authentication support

            return services;
        }

        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Permission-based policies
                foreach (Permission.Permissions permission in Enum.GetValues<Permission.Permissions>())
                {
                    if (permission == Permission.Permissions.None) continue;
                    
                    options.AddPolicy($"Permission.{permission}", policy =>
                        policy.RequireClaim(AuthConstants.PERMISSION_CLAIM_TYPE)
                              .RequireAssertion(context => HasPermission(context, permission)));
                }

                // Combined permission policies
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireAssertion(context => HasPermission(context, Permission.Permissions.SystemAdmin)));

                options.AddPolicy("UserManager", policy =>
                    policy.RequireAssertion(context => 
                        HasPermission(context, Permission.Permissions.UserManager) || 
                        HasPermission(context, Permission.Permissions.SystemAdmin)));

                options.AddPolicy("ProductManager", policy =>
                    policy.RequireAssertion(context => 
                        HasPermission(context, Permission.Permissions.ProductManager) || 
                        HasPermission(context, Permission.Permissions.SystemAdmin)));
            });

            return services;
        }

        private static bool HasPermission(AuthorizationHandlerContext context, Permission.Permissions requiredPermission)
        {
            var permissionsClaim = context.User.FindFirst(AuthConstants.PERMISSION_CLAIM_TYPE);
            if (permissionsClaim == null || !long.TryParse(permissionsClaim.Value, out var userPermissions))
                return false;

            return (userPermissions & (long)requiredPermission) == (long)requiredPermission;
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "JWT API with Bitmask Permissions", 
                    Version = "v1",
                    Description = "ASP.NET Core Web API with JWT Authentication, Windows Authentication, and Bitmask-based Authorization"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference 
                            { 
                                Type = ReferenceType.SecurityScheme, 
                                Id = "Bearer" 
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Add Windows Authentication
                c.AddSecurityDefinition("Windows", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "negotiate",
                    Description = "Windows Authentication"
                });
            });

            return services;
        }

        public static IServiceCollection AddDataAccessServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string is not configured");

            
            services.AddSingleton<IDbConnectionFactory>(provider => 
                new DbConnectionFactory(connectionString));
            
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IBlacklistedTokenRepository, BlacklistedTokenRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() 
                                       ?? new[] { "http://localhost:3000", "https://localhost:3001" };
                    
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });

                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            return services;
        }

        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
                .AddCheck("jwt_service", () => 
                {
                    // Add custom health check for JWT service
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("JWT Service is running");
                });

            return services;
        }
    }