using System;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Models
{
    public class WiserItemFileModel
    {
        public ulong Id { get; set; }

        public ulong ItemId { get; set; }

        public ulong ItemLinkId { get; set; }

        public string ContentType { get; set; }

        [JsonIgnore]
        public byte[] Content { get; set; }

        public string ContentUrl { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public string Title { get; set; }

        public string PropertyName { get; set; }

        public DateTime AddedOn { get; set; }

        public string AddedBy { get; set; }
    }
}
