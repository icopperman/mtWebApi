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
        public SqlCommand cmd;
        public string rawJson1, rawJson2, rawJson3, rawJson4;

        public m.MovieResults mr               = new m.MovieResults();
        public List<string> theaterNames       = new List<string>();
        public List<m.MovieNameObj> movieNames = new List<m.MovieNameObj>();
        public List<m.MovieTime> movieTimes    = new List<m.MovieTime>();

        // GET api/values
        [HttpGet]
//      public IEnumerable<m.TimesWithNameTheater> Post([FromUri] m.ShowTimeReq stq)
        public m.MovieResults Post([FromUri] m.ShowTimeReq stq)
        {
            mr.Status     = "ok";
            mr.ErrMessage = new List<string>();

            //List<m.TimesWithNameTheater> xx = new List<m.TimesWithNameTheater>();

            try
            {
                getTheData(stq);

            }
            catch (Exception ex)
            {
                mr.Status = "fail";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
            }

            //mr.MovieTimes = xx;
            mr.movieNames    = movieNames;
            mr.movieTimesIdx = movieTimes;
            mr.theaterNames  = theaterNames;

            return mr; 

        }

        private void getTheData(m.ShowTimeReq stq)
        {   
            string thedata = "";
            
            List<m.TimesWithNameTheater> allTimesSorted = new List<m.TimesWithNameTheater>();
            //List<m.TimesWithNameTheater> filteredTimes  = new List<m.TimesWithNameTheater>();
            //List<m.TimesWithNameTheater> zz             = new List<m.TimesWithNameTheater>();

            try
            {
                if ((String.IsNullOrEmpty(stq.viewDate) == true) || (String.IsNullOrEmpty(stq.viewZip) == true))
                {
                    throw new Exception("invalid input, no date and/or zip");
                }

                //get data from db
                 GetMovieDataFromDB(stq);

                //if db failure, get directly from web
                if (String.IsNullOrEmpty(rawJson2) == false)
                {
                    mr.Source = "db";
                    //deseiralize data back from db
                    //allTimesSorted = ns.JsonConvert.DeserializeObject<List<m.TimesWithNameTheater>>(rawJson1);
                    movieNames     = ns.JsonConvert.DeserializeObject<List<m.MovieNameObj>>(rawJson2);
                    theaterNames   = ns.JsonConvert.DeserializeObject<List<string>>(rawJson3);
                    movieTimes     = ns.JsonConvert.DeserializeObject<List<m.MovieTime>>(rawJson4);
                }
                else
                {
                    //this gets everything for the entire day
                    thedata = GetMovieDataFromWeb(stq);
                    mr.Source = "web";
                    //reorg data by movie show time, leaves out alot of extraneous info
                    ReorgTheDataByTime(thedata);

                    //put reorg'ed, filtered data to db
                    int rc = PutMovieDataIntoDB(stq);
                }

                //GenerateStats(allTimesSorted);
                //List<m.cTimesWithNameTheater> cAllMovies = CreateCompressedListing(allTimesSorted);

                //int beginViewTime  = Convert.ToInt32(stq.viewBeginTime);
                //int endViewTime    = beginViewTime + Convert.ToInt32(stq.viewEndTime);
                //int movieBeginTime = 0;

                //foreach (m.TimesWithNameTheater xx in allTimesSorted)
                //{
                    //movieBeginTime = Convert.ToInt32(xx.datetime.Substring(0, 2));

                    //if (String.IsNullOrEmpty(stq.viewBeginTime) == false)
                    //{
                    //    //ignore entry if moviebegin time is before time user is interested in
                    //    if (movieBeginTime < beginViewTime) continue;
                    //}

                    ////is there a duration specified?
                    //if (String.IsNullOrEmpty(stq.viewEndTime) == false)
                    //{
                    //    //yes....ignore entry if moviebegin time is after time user is interested in

                    //    if (movieBeginTime > endViewTime) continue;
                    //}

                    ////
                    //if (String.IsNullOrEmpty(stq.titleStartsWith) == false)
                    //{
                    //    if (xx.title.ToLower().StartsWith(stq.titleStartsWith) == false) continue;
                    //}

                //    filteredTimes.Add(xx);

                //}

                //zz = filteredTimes
                //    .OrderBy(mt => mt.datetime)
                //    .ThenBy(mt => mt.title)
                //    .ToList();
            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;

            }

            //return zz;

        }

        private void ReorgTheDataByTime(string thedata)
        {   
            List<m.Movie> themovies                    = new List<m.Movie>();
            List<string> xtheaterNames                 = new List<string>();
            List<string> xmovieNames                   = new List<string>();
            List<string> xxmovieNames                  = new List<string>();

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

                        if (xtheaterNames.Contains(twnt.theTheater) == false)
                        {
                            xtheaterNames.Add(twnt.theTheater);
                        }

                        string xx = twnt.title + "|" + twnt.runTime;

                        if (xxmovieNames.Contains(xx) == false)
                        {
                            xxmovieNames.Add(xx);
                        }

                    }
                }

                theaterNames = xtheaterNames.Distinct().ToList();
                xmovieNames = xxmovieNames.Distinct().ToList();

                theaterNames.Sort();
                xmovieNames.Sort();

                movieNames = xmovieNames
                    .Select(amovie => new m.MovieNameObj { movieName = amovie.Split('|')[0], runTime = amovie.Split('|')[1] })
                    .ToList();
                
                allTimeSorted = allTimes
                       .OrderBy(mt => mt.datetime)
                       .ThenBy(mt => mt.title)
                       .ToList();

                foreach (m.TimesWithNameTheater twnt in allTimeSorted)
                {
                    m.MovieTime amt = new m.MovieTime();

                    int theaterNameIdx = theaterNames.FindIndex(atheaterName => atheaterName == twnt.theTheater);
                    int movieNameidx   = movieNames.FindIndex(amovieObj => amovieObj.movieName == twnt.title);
                    
                    amt.showtime = twnt.datetime;
                    amt.movieNameIdx = movieNameidx.ToString();
                    amt.theaterNameIdx = theaterNameIdx.ToString();

                    movieTimes.Add(amt);
                   
                }

            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

            return;

        }

        private void SetUpSql(string sqlStatement)
        {
            string connStr = ConfigurationManager.ConnectionStrings["MovieTimesConnectionString"].ConnectionString;
            //string connStr     = ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            SqlConnection conn = new SqlConnection(connStr);
            cmd                = new SqlCommand(sqlStatement, conn);
            cmd.CommandText    = sqlStatement;
            cmd.CommandType    = CommandType.Text;
    
            conn.Open();

        }
        
        private void GetMovieDataFromDB(m.ShowTimeReq stq)
        {

            try
            {
                //string sql1 = String.Format("select jsonData      from rawJsonData  where viewDate = '{0}' and viewZip = '{1}'", stq.viewDate, stq.viewZip);
                string sql2 = String.Format("select movieNames    from movieNames   where viewDate = '{0}' and viewZip = '{1}'", stq.viewDate, stq.viewZip);
                string sql3 = String.Format("select theaterNames  from theaterNames where viewDate = '{0}' and viewZip = '{1}'", stq.viewDate, stq.viewZip);
                string sql4 = String.Format("select movieShowings from movieTimes   where viewDate = '{0}' and viewZip = '{1}'", stq.viewDate, stq.viewZip);
                
                //SetUpSql(sql1);
                //Object o   = cmd.ExecuteScalar();
                //if (o != null) rawJson1 = o.ToString();

                SetUpSql(sql2);
                Object o = cmd.ExecuteScalar();
                if (o != null) rawJson2 = o.ToString();
                
                SetUpSql(sql3);
                o = cmd.ExecuteScalar();
                if (o != null) rawJson3 = o.ToString();
                
                SetUpSql(sql4);
                o = cmd.ExecuteScalar();
                if (o != null) rawJson4 = o.ToString();


            }
            catch (Exception ex)
            {
                mr.Status = "fail: ";
                mr.ErrMessage.Add(ex.Message + ", " + ex.StackTrace);
                throw ex;
            }

            //return rawJson1;

        }

        private int PutMovieDataIntoDB(m.ShowTimeReq stq)
        {
            int rc = -1;
            string rawJson = "";
            string sql = "";

            try
            {
                //rawJson = ns.JsonConvert.SerializeObject(allTimeSorted);
                //sql = String.Format("insert into rawJsonData(viewDate, viewZip, jsonData) values('{0}', '{1}', '{2}')", stq.viewDate, stq.viewZip, rawJson);
                //SetUpSql(sql);
                //rc = cmd.ExecuteNonQuery();

                rawJson = ns.JsonConvert.SerializeObject(movieNames);
                sql = String.Format("insert into movieNames(viewDate, viewZip, movieNames) values('{0}', '{1}', '{2}')", stq.viewDate, stq.viewZip, rawJson);
                SetUpSql(sql);
                rc = cmd.ExecuteNonQuery();

                rawJson = ns.JsonConvert.SerializeObject(theaterNames);
                sql = String.Format("insert into theaterNames(viewDate, viewZip, theaterNames) values('{0}', '{1}', '{2}')", stq.viewDate, stq.viewZip, rawJson);
                SetUpSql(sql);
                rc = cmd.ExecuteNonQuery();

                rawJson = ns.JsonConvert.SerializeObject(movieTimes);
                sql = String.Format("insert into movieTimes(viewDate, viewZip, movieShowings) values('{0}', '{1}', '{2}')", stq.viewDate, stq.viewZip, rawJson);
                SetUpSql(sql);
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
                WebResponse wr     = wex.Response;
                Stream ss          = wr.GetResponseStream();
                StreamReader ssr   = new StreamReader(ss);
                string xxx         = ssr.ReadToEnd();
                ns.Linq.JObject jo = ns.Linq.JObject.Parse(xxx);
                dynamic dd         = ns.Linq.JObject.Parse(xxx);

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

        private List<m.cTimesWithNameTheater> CreateCompressedListing(List<m.TimesWithNameTheater> allTimesSorted)
        {
            List<m.cTimesWithNameTheater> cAllMovies = new List<m.cTimesWithNameTheater>();
            string mTime = "";
            string mTitle = "";

            m.cTimesWithNameTheater currEntry = null;
            foreach (m.TimesWithNameTheater amovie in allTimesSorted)
            {

                if (mTime != amovie.datetime)
                {
                    //new movie time, start new entry
                    m.cTimesWithNameTheater cMovie = new m.cTimesWithNameTheater();
                    cMovie.datetime = amovie.datetime;
                    cMovie.title = amovie.title;
                    cMovie.runTime = amovie.runTime;
                    cMovie.theTheaters = new List<string>();
                    cMovie.theTheaters.Add(amovie.theTheater);
                    mTitle = amovie.title;
                    mTime = amovie.datetime;
                    cAllMovies.Add(cMovie);
                    currEntry = cMovie;
                }
                else
                {
                    if (mTitle == amovie.title)
                    {
                        //same movie time, same movie, add to theaters of current entry
                        currEntry.theTheaters.Add(amovie.theTheater);

                    }
                    else
                    {
                        //same movie time, different movie, create new entry
                        m.cTimesWithNameTheater cMovie = new m.cTimesWithNameTheater();
                        cMovie.datetime = amovie.datetime;
                        cMovie.title = amovie.title;
                        cMovie.runTime = amovie.runTime;
                        cMovie.theTheaters = new List<string>();
                        cMovie.theTheaters.Add(amovie.theTheater);
                        mTitle = amovie.title;
                        mTime = amovie.datetime;
                        cAllMovies.Add(cMovie);
                        currEntry = cMovie;
                    }
                }

            }
            return cAllMovies;
        }

        private static void GenerateStats(List<m.TimesWithNameTheater> allTimesSorted)
        {
            int totMovies = allTimesSorted.Count;

            var grouped = allTimesSorted
                .GroupBy(x => (x.title.Length < 20) ? x.title : x.title.Substring(0, 20));

            var groupCount = grouped
                .Select(x => new
                {
                    mt = x.Key,
                    mtCnt = x.Count(),
                    mtPercent = ((float)x.Count() * 100 / totMovies).ToString("F")
                });
            var groupCountOrderMT = groupCount.OrderBy(x => x.mt);
            var groupCountOrderNum = groupCount.OrderByDescending(x => x.mtCnt);

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
