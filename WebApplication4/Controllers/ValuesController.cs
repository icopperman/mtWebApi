using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using m = movietimes;

namespace WebApplication4.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<m.TimesWithNameTheater> Post([FromUri] m.ShowTimeReq stq)
        {
            List<m.TimesWithNameTheater> xx = getTheData(stq);

            return xx; 

        }

        private List<m.TimesWithNameTheater> getTheData(m.ShowTimeReq stq)
        {   
            int begin, moviebegin;
            string thedata = "";
            begin = moviebegin = 0;

            //get data from db or web
            thedata = GetMovieData(stq);

            //if db failure, get directly from web
            if (String.IsNullOrEmpty(thedata) == true)
            {
                thedata = GetDataFromWeb(stq);
            }
            
            List<m.Movie> themovies = JsonConvert.DeserializeObject<List<m.Movie>>(thedata);

            List<m.MovieShowTimes> x = (themovies.Select(movie => new m.MovieShowTimes()
            {
                title     = movie.title,    
                showtimes = movie.showtimes,
                runTime   = movie.runTime

            })).ToList();

            List<m.TimesWithNameTheater> allTimes     = new List<m.TimesWithNameTheater>();
            List<m.TimesWithNameTheater> filteredTimes = new List<m.TimesWithNameTheater>();

            foreach (m.Movie m in themovies)
            {
                foreach (m.Showtime st in m.showtimes)
                {
                    m.TimesWithNameTheater twnt = new m.TimesWithNameTheater();
                    
                    twnt.datetime   = st.dateTime;
                    twnt.theTheatre = st.theatre.name;
                    twnt.title      = m.title;
                    twnt.runTime    = (String.IsNullOrEmpty(m.runTime) == true ) ? "????" : twnt.runTime = m.runTime.Substring(2, 2) + ":" + m.runTime.Substring(5, 2);

                    allTimes.Add(twnt);

                }
            }

            foreach (m.TimesWithNameTheater xx in allTimes)
            {
                string moviestarttime = xx.datetime.Substring(11);
                xx.datetime = moviestarttime;

                if (String.IsNullOrEmpty(stq.viewBeginTime) == false)
                {
                    begin      = Convert.ToInt32(stq.viewBeginTime);
                    moviebegin = Convert.ToInt32(moviestarttime.Substring(0, 2));

                    //ignore entry if moviebegin time is before time user is interested in
                    if (moviebegin < begin) continue;

                }

                //is there a duration specified?
                if (String.IsNullOrEmpty(stq.viewEndTime) == false)
                {
                    //yes....ignore entry if moviebegin time is after time user is interested in
                    int end = begin + Convert.ToInt32(stq.viewEndTime);
                    if (moviebegin > end) continue;
                }

                //
                if (String.IsNullOrEmpty(stq.titleStartsWith) == false)
                {
                    if (xx.title.ToLower().StartsWith(stq.titleStartsWith) == false) continue;
                }

                filteredTimes.Add(xx);

            }

            List<m.TimesWithNameTheater> zz = filteredTimes
                .OrderBy(mt => mt.datetime)
                .ThenBy(mt => mt.title)
                .ToList();

            return zz;

        }

        private string GetMovieData(m.ShowTimeReq stq)
        {
            string rawJson = "";

            try
            {
                string sql         = String.Format("select jsonData from rawJsonData where viewDate = '{0}' and viewZip = '{1}'", stq.viewDate, stq.viewZip);
                string connStr     = ConfigurationManager.ConnectionStrings["MovieTimesConnectionString"].ConnectionString;
                SqlConnection conn = new SqlConnection(connStr);
                SqlCommand cmd     = new SqlCommand(sql, conn);
                
                conn.Open();
                
                Object o = cmd.ExecuteScalar();
                
                if (o != null) rawJson = o.ToString();

                if (String.IsNullOrEmpty(rawJson) == true)
                {
                    rawJson         = GetDataFromWeb(stq);

                    sql             = String.Format("insert into rawJsonData(viewDate, viewZip, jsonData) values('{0}', '{1}', '{2}')",  stq.viewDate, stq.viewZip, rawJson);
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;

                    //sql = "insert into rawJsonData(viewDate, viewZip, jsonData) values('@viewdate', '@viewzip', '@rawJson')";
                    //cmd.Parameters.AddWithValue("@viewdate", this.viewdate.Value);
                    //cmd.Parameters.AddWithValue("@viewzip", this.viewzip.Value);
                    //cmd.Parameters.AddWithValue("@rawJson", rawJson);

                    cmd.ExecuteNonQuery();

                }
            }
            catch (Exception ex)
            {
                string x = ex.Message + "," + ex.StackTrace;
            }

            return rawJson;

        }

        private string GetDataFromWeb(m.ShowTimeReq stq)
        {
            WebRequest wreq;
            WebResponse wresp;
            Stream s;
            StreamReader sr;
     
            string apikey       = "axryg8wt4ajne2ghg5junxbg";
            string baseUrl      = "http://data.tmsapi.com/v1";
            string showtimesUrl = baseUrl + "/movies/showings";
            string zipCode      = stq.viewZip;// "10522";
            string today        = stq.viewDate;
            string radius       = stq.viewMiles;// "10";
            string lat          = stq.viewLat;
            string lon          = stq.viewLon;
            zipCode = "";
            showtimesUrl = (String.IsNullOrEmpty(zipCode) == true ) ?
                 String.Format(showtimesUrl + "?startDate={0}&radius={1}&api_key={2}&lat={3}&lng={4}", today, radius, apikey, lat, lon)
               : String.Format(showtimesUrl + "?startDate={0}&radius={1}&api_key={2}&zip={3}", today, radius, apikey, zipCode);
            
            wreq                = WebRequest.Create(showtimesUrl);
            wreq.Method         = "GET";
            wresp               = wreq.GetResponse();
            s                   = wresp.GetResponseStream();
            
            sr                  = new StreamReader(s);
            string thedata      = sr.ReadToEnd();
            
            string x            = thedata.Replace("'", "''");

            return x;
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        //StringBuilder sb = new StringBuilder();

        //sb.Append("<table rules='all' border='1'>");
        //sb.Append("<tr><th>cnt</th><th>time</th><th>rt</th><th width=400>movie</th><th>theater</th></tr>");

        //string aline = String.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
        //    i.ToString(), moviestarttime, xx.runTime, xx.title, xx.theTheatre);
        //sb.Append(aline);

        //sb.Append("</table>");

        //return sb.ToString();
    }
}
