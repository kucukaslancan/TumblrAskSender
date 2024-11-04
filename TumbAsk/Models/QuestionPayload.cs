using Newtonsoft.Json;

namespace TumbAsk.Models
{
    public class QuestionPayload
    {
        [JsonProperty("content")]
        public Content[] Content { get; set; }

        [JsonProperty("layout")]
        public Layout[] Layout { get; set; }

        [JsonProperty("context")]
        public string Context { get; set; } = "Blog";

        [JsonProperty("state")]
        public string State { get; set; } = "ask";
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "text";

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("formatting")]
        public Formatting[] Formatting { get; set; }
    }

    public class Formatting
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "link";

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Layout
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "ask";

        [JsonProperty("blocks")]
        public int[] Blocks { get; set; }

        [JsonProperty("attribution")]
        public Attribution Attribution { get; set; }
    }

    public class Attribution
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "blog";

        [JsonProperty("blog")]
        public BlogInfo Blog { get; set; }
    }

    public class BlogInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar")]
        public Avatar[] Avatar { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Avatar
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
