using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class NND_Watch
    {
        public Video video { get; set; }
        public object playlist { get; set; }
        public Owner owner { get; set; }
        public object community { get; set; }
        public object mainCommunity { get; set; }
        public object channel { get; set; }
        public object maintenance { get; set; }
        public string watchRecommendationRecipe { get; set; }
        public object series { get; set; }
    }

    public class Video2
    {
        public string video_id { get; set; }
        public int length_seconds { get; set; }
        public int deleted { get; set; }
    }

    public class User
    {
        public int user_id { get; set; }
        public bool is_premium { get; set; }
        public string nickname { get; set; }
    }

    public class AuthTypes
    {
        public string http { get; set; }
        public string hls { get; set; }
    }

    public class Url
    {
        public string url { get; set; }
        public bool is_well_known_port { get; set; }
        public bool is_ssl { get; set; }
    }

    public class SessionApi
    {
        public string recipe_id { get; set; }
        public string player_id { get; set; }
        public List<string> videos { get; set; }
        public List<string> audios { get; set; }
        public List<object> movies { get; set; }
        public List<string> protocols { get; set; }
        public AuthTypes auth_types { get; set; }
        public string service_user_id { get; set; }
        public string token { get; set; }
        public string signature { get; set; }
        public string content_id { get; set; }
        public int heartbeat_lifetime { get; set; }
        public int content_key_timeout { get; set; }
        public double priority { get; set; }
        public List<string> transfer_presets { get; set; }
        public List<Url> urls { get; set; }
    }

    public class Url2
    {
        public string url { get; set; }
        public bool is_well_known_port { get; set; }
        public bool is_ssl { get; set; }
    }

    public class Resolution
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    public class SmileInfo
    {
        public string url { get; set; }
        public bool isSlowLine { get; set; }
        public string currentQualityId { get; set; }
        public List<string> qualityIds { get; set; }
    }

    public class Video
    {
        public string id { get; set; }
        public string title { get; set; }
        public string originalTitle { get; set; }
        public string description { get; set; }
        public string originalDescription { get; set; }
        public string thumbnailURL { get; set; }
        public string largeThumbnailURL { get; set; }
        public string postedDateTime { get; set; }
        public object originalPostedDateTime { get; set; }
        public object width { get; set; }
        public object height { get; set; }
        public int duration { get; set; }
        public int viewCount { get; set; }
        public int mylistCount { get; set; }
        public bool translation { get; set; }
        public object translator { get; set; }
        public string movieType { get; set; }
        public object badges { get; set; }
        public object mainCommunityId { get; set; }
        public object backCommentType { get; set; }
        public object channelId { get; set; }
        public bool isCommentExpired { get; set; }
        public string isWide { get; set; }
        public object isOfficialAnime { get; set; }
        public object isNoBanner { get; set; }
        public bool isDeleted { get; set; }
        public bool isTranslated { get; set; }
        public bool isR18 { get; set; }
        public bool isAdult { get; set; }
        public object isNicowari { get; set; }
        public bool isPublic { get; set; }
        public object isPublishedNicoscript { get; set; }
        public object isNoNGS { get; set; }
        public string isCommunityMemberOnly { get; set; }
        public bool isCommonsTreeExists { get; set; }
        public bool isNoIchiba { get; set; }
        public bool isOfficial { get; set; }
        public bool isMonetized { get; set; }
        public SmileInfo smileInfo { get; set; }
    }

    public class Owner
    {
        public string id { get; set; }
        public string nickname { get; set; }
        public string iconURL { get; set; }
        public object nicoliveInfo { get; set; }
        public object channelInfo { get; set; }
        public bool isUserVideoPublic { get; set; }
        public bool isUserMyVideoPublic { get; set; }
        public bool isUserOpenListPublic { get; set; }
    }
}
