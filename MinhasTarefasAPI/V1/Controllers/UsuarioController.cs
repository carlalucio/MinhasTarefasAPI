using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MinhasTarefasAPI.V1.Models;
using MinhasTarefasAPI.V1.Repositories.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MinhasTarefasAPI.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioRepository _usuarioRepository; //obj para injeção de dependência da interface Usuario
        private readonly SignInManager<ApplicationUser> _signInManager; //obj para injeção de dependência de login do Identity
        private readonly UserManager<ApplicationUser> _userManager; //obj para injeção de dependência do UserManeger
        private readonly IConfiguration _config;//obj para injeçao utilização da chave jwt que está no aquivo de configuração appsettings.json
        private readonly ITokenRepository _tokenRepository;
        public UsuarioController(IUsuarioRepository usuarioRepository, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration config, ITokenRepository tokenRepository)
        {
            _usuarioRepository = usuarioRepository;
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config;
            _tokenRepository = tokenRepository;
        }

        /// <summary>
        /// Operação de Login para gerar o Token de autorização. 
        /// </summary>
        /// <param name="usuarioDTO">Necessário somente E-mail e Senha</param>
        /// <returns>Token de acesso a ser inserido no campo "Authorize"</returns>
        [HttpPost("login")]
        public ActionResult Login([FromBody]UsuarioDTO usuarioDTO)
        {
            //remove os campos nome e confirmação de senha, pois o login só usa email e senha
            ModelState.Remove("Nome");
            ModelState.Remove("ConfirmacaoSenha");
           
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _usuarioRepository.Obter(usuarioDTO.Email, usuarioDTO.Senha);
                if(usuario != null)
                {    //_signInManager.SignInAsync(usuario, false);  --> o login com Identity é Statefull usa o cookie para guardar a autorização

                    //login com JWT retorna só o token criado
                   return GerarToken(usuario);
                }
                else                
                    return NotFound("Usuário não localizado!");
            }
            else            
                return UnprocessableEntity(ModelState);
            
        }

        /// <summary>
        /// Operação para Renovar o Token de acesso
        /// </summary>
        /// <param name="tokenDTO">Token a ser renovado</param>
        /// <returns>Novo token</returns>
        [HttpPost("renovar")]
        public ActionResult Renovar([FromBody] TokenDTO  tokenDTO)
        {
            var refreshTokenDB = _tokenRepository.Obter(tokenDTO.RefreshToken);

            if (refreshTokenDB == null)
                return NotFound();

            //pegar o RefreshToken antigo e atualizar ele - desativar o refresh token
            refreshTokenDB.Atualizado = DateTime.Now;
            refreshTokenDB.Utilizado = true;
            _tokenRepository.Atualizar(refreshTokenDB);

            //Gerar um novo Token/RefreshToken e salvar
            var usuario = _usuarioRepository.Obter(refreshTokenDB.UsuarioId);

            return GerarToken(usuario);

        }

        /// <summary>
        /// Operação para Cadastrar um usuário
        /// </summary>
        /// <param name="usuarioDTO">Dados para cadastro</param>
        /// <returns>Dados de acesso</returns>
       
        [HttpPost("")]
        public ActionResult Cadastrar([FromBody]UsuarioDTO usuarioDTO)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = new ApplicationUser();
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;

                var resultado = _userManager.CreateAsync(usuario, usuarioDTO.Senha).Result;

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var erro in resultado.Errors)
                    {
                        erros.Add(erro.Description);
                    }
                    return UnprocessableEntity(erros);
                }
                else
                    return Ok(usuario);               
            }
            else
                return UnprocessableEntity(ModelState);
        }

        private TokenDTO  BuildToken(ApplicationUser usuario)
        {
            //array de Claims
            var claims = new[]
            {
                //identifica o usuario pelo email
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id)
            };
            //cria a chave e usa o Encoding pq precisa transformar em um array de Bytes
            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-api-jwt-minhas-tarefas"));//Recomendado colocar a chave dentro do appsetting.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])); // chave dentro do appsetting.json
            

            //cria a assinatura passando o algoritmo usado para criptografia
            var sign = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //criar data de expiração do token. Usa o UtcNow para ele adicionar independente do fuso horario do usuario
            var exp = DateTime.UtcNow.AddHours(1);

            //instancia da classe que vai gerar o token. Dentro do construtor passamos os elementos
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: null,                    //indica o emissor do token
                audience: null,                 //indica o dominio que vai consumir esse token
                claims: claims,                 //claims com identificador do usuario
                expires: exp,                   //validade
                signingCredentials: sign        //
                );

            //colocar em uma variavel a instancia do JwtSecurityTokenHandler que recebe as informações do token e gera uma string com a criptografia
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = Guid.NewGuid().ToString();
            var expRefreshToken = DateTime.UtcNow.AddHours(2);
            var tokenDTO = new TokenDTO { Token = tokenString, Expiration = exp, ExpirationRefreshToken = expRefreshToken, RefreshToken = refreshToken };

            return tokenDTO;
        }

        private ActionResult GerarToken(ApplicationUser usuario)
        {
            //gera o token
            var token = BuildToken(usuario);

            //salvar o Token no banco
            var tokenModel = new Token()
            {
                RefreshToken = token.RefreshToken,
                ExpirationToken = token.Expiration,
                ExpirationRefreshToken = token.ExpirationRefreshToken,
                Usuario = usuario,
                Criado = DateTime.Now,
                Utilizado = false

            };
            _tokenRepository.Cadastrar(tokenModel);
            return Ok(token);
        }

    }
}
