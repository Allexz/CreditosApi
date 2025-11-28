using CreditosApi.Interfaces;
using CreditosApi.Models.Queries;
using CreditosApi.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace CreditosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/integrar-credito-constituido")]
    public class CreditosController : ControllerBase
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<CreditosController> _logger;
        private const string TOPIC_NAME = "integrar-credito-constituido-entry";
        private readonly IApplicationBus  _applicationBus;

        public CreditosController(IKafkaProducer kafkaProducer,
                                  ILogger<CreditosController> logger,
                                  IApplicationBus applicationBus)
        {
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationBus = applicationBus;
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> PostAsync([FromBody] ListaCreditosIntegracaoRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.Creditos == null || !request.Creditos.Any())
                {
                    _logger.LogWarning("Requisição inválida: lista de créditos vazia ou nula");
                    return BadRequest("A lista de créditos não pode estar vazia");
                }

                // Serializa o request para JSON
                var messageJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Publica a mensagem no tópico Kafka
                await _kafkaProducer.PublishAsync(TOPIC_NAME, messageJson, cancellationToken);

                _logger.LogInformation(
                    "Créditos publicados no tópico {Topic}. Quantidade: {Count}",
                    TOPIC_NAME,
                    request.Creditos.Count());

                var correlationId = Guid.NewGuid();
                return Accepted(correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar requisição de créditos");
                return StatusCode((int)HttpStatusCode.InternalServerError, "Erro ao processar a requisição");
            }
        }


        [HttpGet("ping")]
        public IActionResult Get()
        {
            return Ok(new { message = "API de Créditos está funcionando!" });
        }


        [HttpGet("numero-credito/{numero_credito}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAsync([FromRoute] string numero_credito, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numero_credito))
                {
                    _logger.LogWarning("Requisição inválida - vazia ou nula");
                    return BadRequest("O número do crédito não pode ser vazio ou nulo");
                }
                var result = await _applicationBus.SendQuery(new CreditoConstituidoConsultaQuery(numero_credito), cancellationToken);
                if (result is null)
                {
                    return NotFound($"Nenhum crédito encontrado para o número fornecido({numero_credito})");
                }

                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar requisição de créditos");
                return StatusCode((int)HttpStatusCode.InternalServerError, "Erro ao processar a requisição");
            }

        }
    }
}