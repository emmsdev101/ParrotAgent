using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace ParrotAgent.Utilities
{
    public class EmbeddingResult
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public Usage TokenUsage { get; set; } = new Usage();

        [JsonPropertyName("data")]
        public EmbeddingData[] Data { get; set; } = [];
    }

    public class Usage
    {
        [JsonPropertyName("totak_tokens")]
        public int TotalTokens { get; set; } = 0;
    }

    public class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }

    public interface IEmbedder
    {
        Task<EmbeddingData[]> GetEmbeddingsAsync(List<string> textsToEmbed);
    }
    public class Embedder : IEmbedder
    {

        
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        public Embedder(IHttpClientFactory httpClient, IConfiguration configuration)
        {
            _clientFactory = httpClient;
            _configuration = configuration;
        }

        public async Task<EmbeddingData[]> GetEmbeddingsAsync(List<string> textsToEmbed)
        {
            HttpClient client = _clientFactory.CreateClient();

            string apiKey = _configuration["EmbeddingApiKey"]??"";
            string apiUrl = _configuration["EmbeddingBaseUrl"]??"";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.PostAsJsonAsync(apiUrl, new
            {
                input = textsToEmbed.ToArray(),
                model = "jina-embeddings-v4",
                dimensions = "1536",
                task = "retrieval.passage",
                late_chunking = true
            });

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get embeddings: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var embeddingResult = await response.Content.ReadFromJsonAsync<EmbeddingResult>();

            var embeddingData = embeddingResult?.Data;

            return embeddingData;
        }
    }
}
