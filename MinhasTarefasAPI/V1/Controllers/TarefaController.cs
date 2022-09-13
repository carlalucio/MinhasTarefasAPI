using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MinhasTarefasAPI.V1.Models;
using MinhasTarefasAPI.V1.Repositories.Contracts;
using System;
using System.Collections.Generic;

namespace MinhasTarefasAPI.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TarefaController : Controller
    {
        private readonly ITarefaRepository _tarefaRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        public TarefaController(ITarefaRepository tarefaRepository, UserManager<ApplicationUser> userManager)
        {
            _tarefaRepository = tarefaRepository;
            _userManager = userManager;
        }
        /// <summary>
        /// Operação para Cadastrar uma Tarefa no banco de dados e Atualizar o Banco de Dados local 
        /// </summary>
        /// <param name="tarefas">Utilize o método "Modelo" para acessar os parâmetros necessários</param>
        /// <returns>Tarefa cadastrada</returns>
        /// 

        [Authorize] //indica que precisa de autenticação para acessar esse método. Ele armazena o registro em Cookies. Para usar token JWT tem que configurar no Startuo.cs o service.AddIdentity
        [HttpPost("sincronizar")]
        public ActionResult Sincronizar([FromBody] List<Tarefa> tarefas)
        {
            return Ok (_tarefaRepository.Sincronizacao(tarefas));
        }


        /// <summary>
        /// Operação que acessa o modelo  de uma tarefa
        /// </summary>
        /// <returns>Modelo de Tarefa</returns>
        [HttpGet("modelo")]
        public ActionResult Modelo() //modelo para receber uma tarefa 
        {
            return Ok(new Tarefa());
        }

        /// <summary>
        /// Operação que pega do banco de dados todas as Informações e Tarefas do Usuário
        /// </summary>
        /// <param name="data">Opcional (Data para consulta)</param>
        /// <returns>Dados do Usuário e Lista de Tarefas</returns>

        [Authorize]
        [HttpGet("restaurar")]
        public ActionResult Restaurar(DateTime data)
        {
            var usuario = _userManager.GetUserAsync(HttpContext.User).Result; //pega o usuario que esta logado
            
            return Ok(_tarefaRepository.Restauracao(usuario, data));
        }

    }
}
