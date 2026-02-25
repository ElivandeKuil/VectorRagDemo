namespace VectorRagDemo.BLL
{
    public static class TextChunker
    {
        public static List<string> ChunkText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            int step = Config.MaxWords - Config.OverlapWords; // 360

            for (int i = 0; i < words.Length; i += step)
            {
                var chunkWords = words.Skip(i).Take(Config.MaxWords).ToArray();
                if (chunkWords.Length > 0)
                    chunks.Add(string.Join(" ", chunkWords));

                if (i + Config.MaxWords >= words.Length)
                    break;
            }

            return chunks;
        }
    }
}
