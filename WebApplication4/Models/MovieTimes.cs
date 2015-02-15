using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace movietimes
{
    public class MovieTimes
    {
        List<Movie> allMovies;

    }
    
    public class ShowTimeReq
    {
        public string viewDate { get; set; }
        public string viewZip { get; set; }
        public string viewMiles { get; set; }
        public string viewBeginTime { get; set; }
        public string viewEndTime { get; set; }
        public string titleStartsWith { get; set; }
        public string viewLat { get; set; }
        public string viewLon { get; set; }
        
    }

    public class MovieShowTimes
    {
        public string title { get; set; }
        public string runTime { get; set; }
        public List<Showtime> showtimes;
    }

    public class MovieResults
    {
        public string Status { get; set; }
        public string Source { get; set; }
        public List<string> ErrMessage { get; set; }
        public List<TimesWithNameTheater> MovieTimes { get; set; }

    }

    public class TimesWithNameTheater
    {
        [JsonProperty("t")]public string title { get; set; }
        [JsonProperty("h")]public string theTheater { get; set; }
        [JsonProperty("d")]public string datetime { get; set; }
        [JsonProperty("r")]public string runTime { get; set; }
    }

    public class cTimesWithNameTheater
    {
        [JsonProperty("d")]public string datetime { get; set; }
        [JsonProperty("t")]public string title { get; set; }
        [JsonProperty("r")]public string runTime { get; set; }
        [JsonProperty("h")]public List<string> theTheaters { get; set; }
    }
    public class Theatre
    {
        public Address address { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Showtime
    {
        public bool barg { get; set; }
        public string dateTime { get; set; }
        public Theatre theatre { get; set; }
        public string ticketURI { get; set; }
        public string quals { get; set; }
    }


    public class Movie
    {
        public string entityType { get; set; }
        public PreferredImage preferredImage { get; set; }
        public string rootId { get; set; }
        public List<Showtime> showtimes { get; set; }
        public string subType { get; set; }
        public string title { get; set; }
        public string titleLang { get; set; }
        public string tmsId { get; set; }
        public string descriptionLang { get; set; }
        public List<string> genres { get; set; }
        public string longDescription { get; set; }
        public string officialUrl { get; set; }
        public List<Rating> ratings { get; set; }
        public string releaseDate { get; set; }
        public int? releaseYear { get; set; }
        public string runTime { get; set; }
        public string shortDescription { get; set; }
        public List<string> topCast { get; set; }
        public QualityRating qualityRating { get; set; }
    }

    public class Caption
    {
        public string content { get; set; }
        public string lang { get; set; }
    }

    public class PreferredImage
    {
        public string uri { get; set; }
        public Caption caption { get; set; }
        public string category { get; set; }
        public string height { get; set; }
        public string primary { get; set; }
        public string width { get; set; }
    }

    public class Address
    {
        public string country { get; set; }
    }

    public class Rating
    {
        public string body { get; set; }
        public string code { get; set; }
    }

    public class QualityRating
    {
        public string ratingsBody { get; set; }
        public string value { get; set; }
    }


}