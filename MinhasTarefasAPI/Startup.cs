using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using MinhasTarefasAPI.Database;
using MinhasTarefasAPI.V1.Helpers.Swagger;
using MinhasTarefasAPI.V1.Models;
using MinhasTarefasAPI.V1.Repositories;
using MinhasTarefasAPI.V1.Repositories.Contracts;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MinhasTarefasAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //configura��o para suprimir a valida��o do ModelState pelo Controller
            services.Configure<ApiBehaviorOptions>(op => {
                op.SuppressModelStateInvalidFilter = true;
            });

            //configura a conex�o com o banco de dados
            services.AddDbContext<MinhasTarefasContext>(op =>
            {
                op.UseSqlite("Data Source=Database\\MinhasTarefas.db");
            });

            //indica para o controller que a interface � que vai injetar a dependencia do repository
            services.AddScoped<IUsuarioRepository, UsuarioRepository>(); 
            services.AddScoped<ITarefaRepository, TarefaRepository>(); 
            services.AddScoped<ITokenRepository, TokenRepository>();

            //configura��o para aceitar formato xml na requisi��o
            services.AddMvc(config=> 
            {
                config.ReturnHttpNotAcceptable = true;  //se colocar um formato n�o suportado, retorna erro 406
                config.InputFormatters.Add(new XmlSerializerInputFormatter(config));    //formato da requisi��o suportada
                config.OutputFormatters.Add(new XmlSerializerOutputFormatter());        //foramato da resposta suportada
            })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                //configura��o para o restaurar() tarefa n�o ficar em loop
                .AddJsonOptions( 
                        options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    );

            //adicionar servico de versaionamento de API
            services.AddApiVersioning(cfg =>
            {
                //essa fun��o retorna no cabe�alho quais as vers�es suportadas e dispon�veis quando for feita uma requisi��o
                cfg.ReportApiVersions = true;

                //cfg.ApiVersionReader = new HeaderApiVersionReader("api-version"); //adiciona o leitor de vers�o pelo cabe�alho da requis�o
                cfg.AssumeDefaultVersionWhenUnspecified = true;  //direciona o usu�rio para a vers�o padr�o caso n�o seja especificado isso na url
                cfg.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0); //indica a vers�o padr�o 
            });

            //configurando o Swagger
            services.AddSwaggerGen(cfg =>
            {
                //configura��o para o swagger habilitar a inclus�o do Token na request - usar o JWT
                cfg.AddSecurityDefinition("Bearer", new ApiKeyScheme(){
                    In = "header", //onde est� localizado o processo de autentica��o
                    Type = "apiKey",
                    Description = "Adicione o JSON Web Token(JWT) para autenticar.",
                    Name = "Authorization" 
                });

                //cria um dicion�rio para receber o token - array de string
                var security = new Dictionary<string, IEnumerable<string>>() {
                    {"Bearer", new string[] { } }                
                };
                cfg.AddSecurityRequirement(security);


                cfg.ResolveConflictingActions(apiDescription => apiDescription.First());//se tiver conflito de rota ele pega o primeiro

                //1� coloca a vers�o, 2� instancia classe Info() e coloca parametros {titulo, versao}            
                cfg.SwaggerDoc("v1.0", new Swashbuckle.AspNetCore.Swagger.Info()
                {
                    Title = "MinhasTarefasAPI - V1.0",
                    Version = "v1.0"
                });
               

                //criando vari�veis que usam o platformServices para pegar o caminho e o nome do arquivo xml com os coment�rios
                var CaminhoProjeto = PlatformServices.Default.Application.ApplicationBasePath;
                var NomeProjeto = $"{PlatformServices.Default.Application.ApplicationName}.xml";
                var CaminhoAquivoXMLComentario = Path.Combine(CaminhoProjeto, NomeProjeto);

                //configura��o para o swagger usar os coment�rios feitos no controller
                cfg.IncludeXmlComments(CaminhoAquivoXMLComentario);


                //configura��o para selecionar qual vers�o quer exibir
                cfg.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
                    // would mean this action is unversioned and should be included everywhere
                    if (actionApiVersionModel == null)
                    {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
                });
                cfg.OperationFilter<ApiVersionOperationFilter>(); //passa a classe para filtrar a vers�o
            });


            // adiciona o Iconfiguration para acessar o arquivo de configura��o e o JWT ter acesso a chave
            services.AddSingleton<IConfiguration>(Configuration);

            //adiciona o servi�o de redirecionamento para autentica��o por login pelo Identity 
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MinhasTarefasContext>()
                    .AddDefaultTokenProviders(); //habilita o uso de tokens ao inv�s de cookies

           

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;//schema de autentica��o padr�o do jwt
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters() //quais parametros de um token vai validar 
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,                   
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])) //pega a chave do aquivo appsettings.json

                };
            });

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                                                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme) //inidca qual schema de verifica��o
                                                .RequireAuthenticatedUser()         //verifica o usuario
                                                .Build()
                );
            });

            //adiciona  o tratamento da tela de redirecionamento de login, caso o usuario tente fazer uma solicita��o sem autoriza��o
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });

            services.Configure<CookiePolicyOptions>(options => {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
               
                app.UseHsts();
            }
            app.UseAuthentication(); //indica a utiliza��o do jwt token para a��es com autoriza��o
            app.UseStatusCodePages();
            app.UseHttpsRedirection();
            app.UseMvc();

            // cria o arquivo base --> /swagger/v1/swagger.json
            app.UseSwagger(); 

            //gera a interface gr�fica do Swagger e passa a configura��o qual ser� o endpoint e o nome da API
            app.UseSwaggerUI(cfg =>
            {
                cfg.SwaggerEndpoint("/swagger/v1.0/swagger.json", "MinhasTarefasAPI V1.0");                
                cfg.RoutePrefix = String.Empty; //configura��o para que ao acessar a raiz da api direcione para o swaggerUI
            });

            //app.UseStaticFiles();
            //app.UseCookiePolicy();


        }
    }
}
