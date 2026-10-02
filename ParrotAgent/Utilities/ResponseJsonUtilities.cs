using ParrotAgent.Models;

namespace ParrotAgent.Utilities
{
    public class ResponseAsk()
    {
        public string Result { get; set; }
        public List<DocumentChunk> DocumentChunks { get; set; }
    }
}
