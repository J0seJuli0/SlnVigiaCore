using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PrjVigiaCore.Services
{
    public interface ITokenService
    {
        string GenerarToken(string idCliente);
        string ObtenerIdCliente(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly ConcurrentDictionary<string, (string IdCliente, DateTime Expira)> _tokens;
        private readonly TimeSpan _tiempoExpiracion = TimeSpan.FromMinutes(30);

        public TokenService()
        {
            _tokens = new ConcurrentDictionary<string, (string, DateTime)>();
        }

        public string GenerarToken(string idCliente)
        {
            var token = Guid.NewGuid().ToString("N");
            var expira = DateTime.UtcNow.Add(_tiempoExpiracion);
            _tokens[token] = (idCliente, expira);
            return token;
        }

        public string ObtenerIdCliente(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            if (_tokens.TryGetValue(token, out var valor) && valor.Expira > DateTime.UtcNow)
            {
                return valor.IdCliente;
            }
            return null;
        }
    }
}