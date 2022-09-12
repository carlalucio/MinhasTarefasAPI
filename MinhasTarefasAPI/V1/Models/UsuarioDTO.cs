using System.ComponentModel.DataAnnotations;

namespace MinhasTarefasAPI.V1.Models
{
    //esse usuario não vai para o banco de daos. Vamos usá-lo para receber o formulário para cadastro e login de usuario
    public class UsuarioDTO
    {
        [Required]
        public string Nome { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Senha { get; set; }
        [Required]
        [Compare("Senha")]
        public string ConfirmacaoSenha { get; set; }
    }
}
