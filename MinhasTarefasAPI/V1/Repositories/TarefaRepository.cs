using MinhasTarefasAPI.Database;
using MinhasTarefasAPI.V1.Models;
using MinhasTarefasAPI.V1.Repositories.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinhasTarefasAPI.V1.Repositories
{
    public class TarefaRepository : ITarefaRepository
    {

        private readonly MinhasTarefasContext _banco;
        public TarefaRepository(MinhasTarefasContext banco)
        {
            _banco = banco;
        }

        public List<Tarefa> Restauracao(ApplicationUser usuario, DateTime dataUltimaSincronizacao)
        {
            var query = _banco.Tarefas.Where(a=>a.UsuarioId == usuario.Id).AsQueryable();
            
            if(dataUltimaSincronizacao != null)            
                query.Where(a=>a.Criado >= dataUltimaSincronizacao || a.Atualizado >= dataUltimaSincronizacao);
            
            return query.ToList<Tarefa>();
        }

        public List<Tarefa> Sincronizacao(List<Tarefa> tarefas)
        {
            
            var tarefasNovas = tarefas.Where(a => a.IdTarefaApi == 0).ToList();
            var tarefasExcluidasAtualizadas = tarefas.Where(a => a.IdTarefaApi != 0).ToList();

            //Cadastrar novos registros no banco de dados da API
            if (tarefasNovas.Count() > 0)
            {
                foreach (var tarefa in tarefasNovas)
                {
                    _banco.Tarefas.Add(tarefa);
                }
                
            }

            //Atualização de registro (excluido)           
            if (tarefasExcluidasAtualizadas.Count() > 0)
            {
                foreach (var tarefa in tarefasExcluidasAtualizadas)
                {
                    _banco.Tarefas.Update(tarefa);
                }
                
            }

            _banco.SaveChanges();
            return tarefasNovas.ToList();
        }



        

        
    }
}
