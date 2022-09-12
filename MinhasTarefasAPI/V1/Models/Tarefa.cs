﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinhasTarefasAPI.V1.Models
{
    /*
     * 
     */
    public class Tarefa
    {
        [Key]
        public int IdTarefaApi { get; set; } //id da tarefa para banco geral da API
        public int IdTarefaApp { get; set; }//id da tarefa para o banco individual do usuario
        public string  Titulo { get; set; }
        public DateTime DataHora { get; set; }
        public string Local { get; set; }
        public string Descricao { get; set; }
        public string Tipo { get; set; }
        public bool Concluido { get; set; }
        public bool Excluido { get; set; }
        public DateTime Criado { get; set; }
        public DateTime Atualizado { get; set; }



        [ForeignKey("Usuario")]
        public string UsuarioId { get; set; }

        public virtual ApplicationUser Usuario { get; set; }
    }
}
