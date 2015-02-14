using ns=Newtonsoft.Json;
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
        public m.MovieResults mr = new m.MovieResults();

        // GET api/values
        [HttpGet]
//      public IEnumerable<m.TimesWithNameTheater> Post([FromUri] m.ShowTimeReq stq)
        public m.MovieResults Post([FromUri] m.ShowTimeReq stq)
        {
            mr.Status = "ok";
            mr.ErrMessage = new List<string>();

            List<m.TimesWithNameTheater> xx = new List<m.TimesWithNameTheater>();

            try
            {
                xx = getTheData(stq);

            }
            catch (Exception ex)
            {
                mr.Status = "fail";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
            }

            mr.MovieTimes = xx;
            return mr; 

        }

        private List<m.TimesWithNameTheater> getTheData(m.ShowTimeReq stq)
        {   
            string thedata = "";
            
            List<m.TimesWithNameTheater> allTimesSorted = new List<m.TimesWithNameTheater>();
            List<m.TimesWithNameTheater> filteredTimes  = new List<m.TimesWithNameTheater>();
            List<m.TimesWithNameTheater> zz             = new List<m.TimesWithNameTheater>();

            try
            {
                //get data from db
                thedata = GetMovieDataFromDB(stq);

                //if db failure, get directly from web
                if (String.IsNullOrEmpty(thedata) == false)
                {
                    mr.Source = "db";
                    //deseiralize data back from db
                    allTimesSorted = ns.JsonConvert.DeserializeObject<List<m.TimesWithNameTheater>>(thedata);
                }
                else
                {
                    //this gets everything for the entire day
                    thedata = GetMovieDataFromWeb(stq);
                    mr.Source = "web";
                    //reorg data by movie show time, leaves out alot of extraneous info
                    allTimesSorted = ReorgTheDataByTime(thedata);

                    //put reorg'ed, filtered data to db
                    int rc = PutMovieDataIntoDB(stq, allTimesSorted);
                }

                int totMovies = allTimesSorted.Count;

                var grouped = allTimesSorted
                    .GroupBy(x => ( x.title.Length < 20 ) ? x.title : x.title.Substring(0,20));

                var groupCount = grouped
                    .Select(x => new { mt = x.Key, 
                        mtCnt = x.Count(),
                        mtPercent = ((float)x.Count()*100 / totMovies).ToString("F")
                    });
                var groupCountOrderMT = groupCount.OrderBy(x => x.mt);
                var groupCountOrderNum = groupCount.OrderByDescending(x => x.mtCnt);

                int beginViewTime  = Convert.ToInt32(stq.viewBeginTime);
                int endViewTime    = beginViewTime + Convert.ToInt32(stq.viewEndTime);
                int movieBeginTime = 0;

                foreach (m.TimesWithNameTheater xx in allTimesSorted)
                {
                    movieBeginTime = Convert.ToInt32(xx.datetime.Substring(0, 2));

                    if (String.IsNullOrEmpty(stq.viewBeginTime) == false)
                    {
                        //ignore entry if moviebegin time is before time user is interested in
                        if (movieBeginTime < beginViewTime) continue;
                    }

                    //is there a duration specified?
                    if (String.IsNullOrEmpty(stq.viewEndTime) == false)
                    {
                        //yes....ignore entry if moviebegin time is after time user is interested in

                        if (movieBeginTime > endViewTime) continue;
                    }

                    //
                    if (String.IsNullOrEmpty(stq.titleStartsWith) == false)
                    {
                        if (xx.title.ToLower().StartsWith(stq.titleStartsWith) == false) continue;
                    }

                    filteredTimes.Add(xx);

                }

                zz = filteredTimes
                    .OrderBy(mt => mt.datetime)
                    .ThenBy(mt => mt.title)
                    .ToList();
            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;

            }

            return zz;

        }

        private List<m.TimesWithNameTheater> ReorgTheDataByTime(string thedata)
        {   
            List<m.Movie> themovies                    = new List<m.Movie>();
            List<m.TimesWithNameTheater> allTimes      = new List<m.TimesWithNameTheater>();
            List<m.TimesWithNameTheater> allTimeSorted = new List<m.TimesWithNameTheater>();

            try
            {
                themovies = ns.JsonConvert.DeserializeObject<List<m.Movie>>(thedata);

                //flatten Movie structure, organized by movie, into structure organized by time
                foreach (m.Movie m in themovies)
                {
                    foreach (m.Showtime st in m.showtimes)
                    {
                        m.TimesWithNameTheater twnt = new m.TimesWithNameTheater();
                        int idx                     = st.dateTime.IndexOf("T");
                        string x                    = st.dateTime.Substring(idx + 1);
                        twnt.datetime               = x;// st.dateTime;
                        twnt.theTheater             = st.theatre.name;
                        twnt.title                  = m.title;
                        twnt.runTime                = (String.IsNullOrEmpty(m.runTime) == true) ? "????" : twnt.runTime = m.runTime.Substring(2, 2) + ":" + m.runTime.Substring(5, 2);

                        allTimes.Add(twnt);

                    }
                }

                allTimeSorted = allTimes
                       .OrderBy(mt => mt.datetime)
                       .ThenBy(mt => mt.title)
                       .ToList();

            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

            return allTimeSorted;

        }

        private string GetMovieDataFromDB(m.ShowTimeReq stq)
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

            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

            return rawJson;

        }

        private int PutMovieDataIntoDB(m.ShowTimeReq stq, List<m.TimesWithNameTheater> allTimeSorted)
        {
            int rc = -1;

            try
            {
                string rawJson     = ns.JsonConvert.SerializeObject(allTimeSorted);
                string sql         = String.Format("insert into rawJsonData(viewDate, viewZip, jsonData) values('{0}', '{1}', '{2}')", stq.viewDate, stq.viewZip, rawJson);
                string connStr     = ConfigurationManager.ConnectionStrings["MovieTimesConnectionString"].ConnectionString;
                SqlConnection conn = new SqlConnection(connStr);
                SqlCommand cmd     = new SqlCommand(sql, conn);
                cmd.CommandText    = sql;
                cmd.CommandType    = CommandType.Text;
                
                conn.Open();
                rc = cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

            return rc;
            
        }

        private string GetMovieDataFromWeb(m.ShowTimeReq stq)
        {
            WebRequest wreq;
            WebResponse wresp;
            Stream s;
            StreamReader sr;
            string x = "";
            string thedata = "";

            try
            {
                //string apikey       = "axryg8wt4ajne2ghg5junxbg"; //v1
                string apikey       = "d2er6ess8g5eccjhju6puy5p"; //v1.1
                string baseUrl      = "http://data.tmsapi.com/v1.1";
                string showtimesUrl = baseUrl + "/movies/showings";
                string zipCode      = stq.viewZip;// "10522";
                string today        = stq.viewDate;
                string radius       = stq.viewMiles;// "10";
                string lat          = stq.viewLat;
                string lon          = stq.viewLon;
                
                //zipCode = "";
                showtimesUrl = (String.IsNullOrEmpty(zipCode) == true) ?
                     String.Format(showtimesUrl + "?startDate={0}&radius={1}&api_key={2}&lat={3}&lng={4}", today, radius, apikey, lat, lon)
                   : String.Format(showtimesUrl + "?startDate={0}&radius={1}&api_key={2}&zip={3}", today, radius, apikey, zipCode);

                wreq        = WebRequest.Create(showtimesUrl);
                wreq.Method = "GET";

                wresp       = wreq.GetResponse();
                s           = wresp.GetResponseStream();
                
                sr          = new StreamReader(s);
                thedata     = sr.ReadToEnd();
                
                x           = thedata.Replace("'", "''");
            }
            catch (WebException wex)
            {
                WebResponse wr = wex.Response;
                Stream ss = wr.GetResponseStream();
                StreamReader ssr = new StreamReader(ss);
                string xxx = ssr.ReadToEnd();
                ns.Linq.JObject jo = ns.Linq.JObject.Parse(xxx);
                dynamic dd = ns.Linq.JObject.Parse(xxx);

                mr.Status = "fail";
                mr.ErrMessage.Add(wex.Message + "," + jo["errorCode"] + "," + jo["errorMessage"] + "," + wex.StackTrace);
                throw wex;

            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

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

        //List<m.MovieShowTimes> x = (themovies.Select(movie => new m.MovieShowTimes()
        //{
        //    title = movie.title,
        //    showtimes = movie.showtimes,
        //    runTime = movie.runTime

        //})).ToList();
    }
}
