﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using JMusik.Data;
using JMusik.Data.Contratos;
using JMusik.Data.Repositorios;
using JMusik.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace JMusik.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddDbContext<TiendaDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("TiendaDb")));

            services.AddScoped<IRepositorioGenerico<Perfil>, RepositorioPerfiles>();
            services.AddScoped<IProductosRepositorio, ProductosRepositorio>();
            services.AddScoped<IOrdenesRepositorio, RepositorioOrdenes>();

            services.AddScoped<IUsuariosRepositorio, RepositorioUsuarios>();

            services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();


            //Accedemos a la sección JwtSettings del archivo appsettings.json
            var jwtSettings = Configuration.GetSection("JwtSettings");
            //Obtenemos la clave secreta guardada en JwtSettings:SecretKey
            string secretKey = jwtSettings.GetValue<string>("SecretKey");
            //Obtenemos el tiempo de vida en minutos del Jwt guardada en JwtSettings:MinutesToExpiration
            int minutes = jwtSettings.GetValue<int>("MinutesToExpiration");
            //Obtenemos el valor del emisor del token en JwtSettings:Issuer
            string issuer = jwtSettings.GetValue<string>("Issuer");
            //Obtenemos el valor de la audiencia a la que está destinado el Jwt en JwtSettings:Audience
            string audience = jwtSettings.GetValue<string>("Audience");

            var key = Encoding.ASCII.GetBytes(secretKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(minutes)
                };
            });


            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseAuthentication();
            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
