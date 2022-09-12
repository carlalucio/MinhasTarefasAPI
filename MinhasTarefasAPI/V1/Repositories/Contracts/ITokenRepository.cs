
using MinhasTarefasAPI.V1.Models;

namespace MinhasTarefasAPI.V1.Repositories.Contracts
{
    public interface ITokenRepository
    {
        void Cadastrar(Token token);

        Token Obter(string refreshToken);

        void Atualizar(Token token);


    }
}
