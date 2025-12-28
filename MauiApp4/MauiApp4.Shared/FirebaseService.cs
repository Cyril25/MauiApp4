using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Services
{
    public class FirebaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId = "asso-billet-site"; // remplacer par ton project ID
        private readonly string _baseUrl;

        public FirebaseService()
        {
            _httpClient = new HttpClient();
            _baseUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents";
        }

        // GET documents d'une collection
        public async Task<List<WhitelistItem>> GetWhitelistAsync()
        {
            var url = $"{_baseUrl}/whitelist";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var items = new List<WhitelistItem>();

            if (doc.RootElement.TryGetProperty("documents", out var documents))
            {
                foreach (var docElement in documents.EnumerateArray())
                {
                    // Extraire l'ID (qui est l'email)
                    var fullName = docElement.GetProperty("name").GetString();
                    var email = fullName?.Split('/').Last() ?? string.Empty;

                    // Extraire le rôle
                    var role = string.Empty;
                    if (docElement.TryGetProperty("fields", out var fields) &&
                        fields.TryGetProperty("role", out var roleProp) &&
                        roleProp.TryGetProperty("stringValue", out var roleValue))
                    {
                        role = roleValue.GetString() ?? string.Empty;
                    }

                    items.Add(new WhitelistItem
                    {
                        Email = email,
                        Role = role
                    });
                }
            }

            return items;
        }

        public class WhitelistItem
        {
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }


        // CREATE document
        public async Task<JsonDocument> AddDocumentAsync(string collectionName, object data)
        {
            var url = $"{_baseUrl}/{collectionName}";
            var content = JsonContent.Create(new { fields = ConvertToFirestoreFormat(data) });
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }

        // UPDATE document
        public async Task<JsonDocument> UpdateDocumentAsync(string collectionName, string docId, object data)
        {
            var url = $"{_baseUrl}/{collectionName}/{docId}?currentDocument.exists=true";
            var content = JsonContent.Create(new { fields = ConvertToFirestoreFormat(data) });
            var response = await _httpClient.PatchAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }

        // DELETE document
        public async Task DeleteDocumentAsync(string collectionName, string docId)
        {
            var url = $"{_baseUrl}/{collectionName}/{docId}";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }

        // Conversion simple pour Firestore (string et number seulement ici pour exemple)
        private static object ConvertToFirestoreFormat(object obj)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in obj.GetType().GetProperties())
            {
                var value = prop.GetValue(obj);
                if (value is string s)
                    dict[prop.Name] = new { stringValue = s };
                else if (value is int i)
                    dict[prop.Name] = new { integerValue = i.ToString() };
                else if (value is double d)
                    dict[prop.Name] = new { doubleValue = d };
            }
            return dict;
        }
    }
}
