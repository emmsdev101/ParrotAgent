using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;

namespace ParrotAgent.Utilities
{
    public interface ILLM
    {
       Task<string> Chat(List<ChatMessage> chatMessages);
        Task<string> RagChat(List<string> contexts, string userQuestion);
    }
    public class LLM : ILLM
    {
        private readonly ChatClient _chatClient;
        private readonly string _systemPromt =
            "You are an expert document management clerk."+
            "Given the following context, answer the users's query."+
            "If context is empty or does not contain the answer, do not use yor general knowledge;"+
            "respond with \"I cant find a relevant context to your question\"";
        public LLM()
        {
            _chatClient = new ChatClient(
                model:  "deepseek-chat",
                credential:new System.ClientModel.ApiKeyCredential("sk-599462dd367b43399b9cf2c2073b6e15"),
                options: new OpenAI.OpenAIClientOptions
                {
                    Endpoint = new Uri("https://api.deepseek.com/v1")
                }
                );
        }

        public async Task<string> Chat(List<ChatMessage> chatMessages)
        {
            ChatCompletion chat = await _chatClient.CompleteChatAsync(chatMessages);
            return chat.Content[0].Text;
        }

        public async Task<string> RagChat(List<string> contexts, string userQuestion)
        {
            List<ChatMessage> chatMessages = new List<ChatMessage>();

            chatMessages.Add(new SystemChatMessage(_systemPromt));
            string context = "";
            foreach(var c in contexts)
            {
                context += c + "\n";
            }
            chatMessages.Add(new UserChatMessage(
                $"<context>\n {context}</context>\n\n"+
                $"User Query: {userQuestion}"
                ));

            return await this.Chat(chatMessages);
        }
    }
}
