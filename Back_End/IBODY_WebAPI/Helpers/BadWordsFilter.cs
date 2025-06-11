using System.Text.Json;

namespace IBODY_WebAPI.Helpers 
{
    public static class BadWordsFilter
    {
        private static List<string> _badWords = new();

        // Gọi khi ứng dụng khởi động
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var json = File.ReadAllText(filePath);
            _badWords = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        // Gọi mỗi lần muốn kiểm tra nội dung
        public static bool ContainsBadWords(string content)
        {
            var lower = content.ToLower();
            return _badWords.Any(word => lower.Contains(word));
        }
    }
}
