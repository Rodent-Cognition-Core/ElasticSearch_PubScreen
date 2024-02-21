using AngularSPAWebAPI.Models;
using AngularSPAWebAPI.Services;
using CBAS.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Core.TermVectors;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Fuzziness = Elastic.Clients.Elasticsearch.Fuzziness;

namespace ElasticSearch_PubScreen
{
    internal class ElasticSearchPubScreen
    {
        private static ElasticClient client = null;


        private ConnectionSettings settings = new ConnectionSettings(new Uri("https://localhost:9200"))
                           .CertificateFingerprint("be52412c000807283da52f26ffc9a5f7771e84ff8a2fc9bb8f757388faf2d411")
                           .BasicAuthentication("elastic", "vQJmdYe1MKn9JCB-O=S3");

        public ElasticSearchPubScreen()
        {


            try
            {
                client = new ElasticClient(settings);
                settings.DefaultIndex("pubscreen");

            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error setting up elastic search, the following error occured " + ex.Message);
            }

        }
        public void createIndices()
        {

            var partialName = new Elastic.Clients.Elasticsearch.Analysis.CustomAnalyzer
            {
                Filter = new List<string> { "lowercase", "name_ngrams", "standard", "asciifolding", "trim", "word_delimiter" },
                Tokenizer = "lowercase"

            };

            var fullName = new Elastic.Clients.Elasticsearch.Analysis.CustomAnalyzer
            {
                Filter = new List<string> { "standard", "lowercase", "asciifolding", "word_delimiter" },
                Tokenizer = "lowercase"
            };

            client.Indices.Create("pubscreen", c => c
                .Settings(s => s
                    .Analysis(descriptor => descriptor
                        .TokenFilters(bases => bases
                        .NGram("name_ngrams", td => td
                        .MinGram(2)
                        .MaxGram(10))
                        .Lowercase("partial_name")
                        )
                     )
                  )
               );
        }
        public ElasticClient GetElasticsearchClient()
        {
            return client;
        }

        public QueryContainer tempQuery(PubScreen pubscreen, QueryContainerDescriptor<PubScreenSearch> query) => query.DisMax(dx => dx
                    .Queries(dxq => dxq
                                .Match(dxqm => dxqm
                                .Field(f => f.Title)
                                .Query(pubscreen.search)
                                .Fuzziness(Nest.Fuzziness.Auto)),
                            dxq => dxq
                                .Match(dxqm => dxqm
                                .Field(f => f.Author)
                                .Query(pubscreen.search)
                                .Fuzziness(Nest.Fuzziness.Auto)),
                            dxq => dxq
                                .Bool(boolq => boolq
                                .Should(boolShould => boolShould
                                .Wildcard(dxqm => dxqm
                                .Field(f => f.Keywords)
                                .Value("*" + pubscreen.search + "*")))),
                            dxq => dxq
                                .Bool(boolq => boolq
                                .Should(boolShould => boolShould
                                .Wildcard(dxqm => dxqm
                                .Field(f => f.Title)
                                .Value("*" + pubscreen.search + "*")))),

                            dxq => dxq
                                .Match(dxqm => dxqm
                                .Field(f => f.Author)
                                .Query(pubscreen.Author))));
        public QueryContainer ApplyQuery(PubScreen pubscreen, QueryContainerDescriptor<PubScreenSearch> query)
        {
            var searchQuery = query.DisMax(dxq => dxq.Queries(JoinAllQuries(pubscreen, query).ToArray()));
            var stringQuery = client.RequestResponseSerializer.SerializeToString(searchQuery);
            Console.WriteLine("this is dynamically generated query: ", stringQuery);

            //var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
            //    .Size(10000)

            //.Query(q => tempQuery(pubscreen, q)));

            QueryContainerDescriptor<PubScreenSearch> test_temp = new QueryContainerDescriptor<PubScreenSearch>();
            var temp = tempQuery(pubscreen, test_temp);

            var stringTempQuery = client.RequestResponseSerializer.SerializeToString(temp);
            Console.WriteLine("this is hardCoded query ", stringTempQuery)

            /*stringQuery = client.RequestResponseSerializer.SerializeToString()*/;
            //Console.WriteLine("this is dynamically generated query: ", query);
            return searchQuery;
        }

        private List<QueryContainer> JoinAllQuries(PubScreen pubscreen, QueryContainerDescriptor<PubScreenSearch> query)
        {
            var container = new List<QueryContainer>();

            var multiMatchQuery = MultiMatchSearchField(pubscreen, query);
            if (multiMatchQuery.Count > 0)
            {
                foreach (var multiQuery in multiMatchQuery)
                {
                    container.Add(multiQuery);
                }
                
            }

            var otherFieldQuery = OtherFieldMatch(pubscreen, query);

            if(otherFieldQuery.Count > 0)
            {
                foreach(var otherField in otherFieldQuery)
                {
                    container.Add(otherField);
                }
            }
            return container;
        }
        public List<QueryContainer> MultiMatchSearchField(PubScreen pubscreen, QueryContainerDescriptor<PubScreenSearch> query) =>
            string.IsNullOrEmpty(pubscreen.search) ? new List<QueryContainer>() : ApplyMatchQuery(pubscreen.search, query);


        private List<QueryContainer> ApplyMatchQuery(string searchingFor, QueryContainerDescriptor<PubScreenSearch> query) 
        {
            var queryContainer = new List<QueryContainer>();

            var titleQuery = TitleMatch(searchingFor, query);
            if(titleQuery.Count > 0)
            {
                foreach (var title in titleQuery)
                {
                    queryContainer.Add(title);
                }
                
            }
               
            

            foreach( var container in KeyWordMatch(searchingFor, query))
            {
                queryContainer.Add(container);
            }

            foreach(var container in AurthorMatch(searchingFor, query))
            {
                queryContainer.Add(container);
            }

            //(+TitleMatch(searchingFor, query) && && );
            return queryContainer;
         }
        private List<QueryContainer> TitleMatch(string searchingFor, QueryContainerDescriptor<PubScreenSearch> query)
        {

            return (MatchRelevance(searchingFor, query, x => x.Title ));
        }

        private List<QueryContainer> KeyWordMatch(string searchingFor, QueryContainerDescriptor<PubScreenSearch> query)
        {
            return (MatchRelevance(searchingFor, query, x => x.Keywords));
        }

        private List<QueryContainer> AurthorMatch(string searchingFor, QueryContainerDescriptor<PubScreenSearch> query)
        {
            return (MatchRelevance(searchingFor, query, x => x.Author));
        }

        private List<QueryContainer> MatchRelevance(object searchingFor, QueryContainerDescriptor<PubScreenSearch> query, Func<PubScreenSearch, IComparable> getProp)
        {
            var queryContainer = new List<QueryContainer>();
            queryContainer.Add(MatchWithFuzziness(searchingFor, query, x => getProp(x)));
            //queryContainer.Add(MatchWithWildCard(searchingFor, query, fieldName));
            return queryContainer;    
        }

        private QueryContainer MatchWithFuzziness(object searchingFor, QueryContainerDescriptor<PubScreenSearch> query, Func<PubScreenSearch, IComparable> getProp)
        {
            return query
                        .Match(dxqm => dxqm
                        .Field(f => getProp(f))
                        .Query(searchingFor.ToString())
                        .Fuzziness(Nest.Fuzziness.Auto)
                    );
        }
            

        private QueryContainer MatchWithWildCard(object searchingFor, QueryContainerDescriptor<PubScreenSearch> query, string fieldName) => query
        .Bool(boolq => boolq
                        .Should(boolShould => boolShould
                        .Wildcard(dxqm => dxqm
                        .Field(f => fieldName)
                        .Value("*" + searchingFor.ToString() + "*"))));
        private object getPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private QueryContainer EaxctMatch(object searchingFor, QueryContainerDescriptor<PubScreenSearch> query,   string fieldName) => +query
            .Match(m => m
            .Field(fieldName)
            .Boost(1000)
            .Query(searchingFor.ToString()));

        private List<QueryContainer> OtherFieldMatch(PubScreen pubscreen, QueryContainerDescriptor<PubScreenSearch> query)
        {
            var queryContainer = new List<QueryContainer>();
            foreach (PropertyInfo pi in pubscreen.GetType().GetProperties())
            {
                if(pi.Name == "search")
                {
                    continue;
                }
                string value = Convert.ToString(pi.GetValue(pubscreen, null));
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }
                if (pi.PropertyType == typeof(string))
                {
                    //foreach(var container in MatchRelevance(value, query, pi.Name))
                    //{
                    //    queryContainer.Append(container);
                    //}
                   
                }
                else
                {

                        queryContainer.Append(EaxctMatch(value, query, pi.Name));
                    
                }
                
            }
            return queryContainer;
        }

        private bool IsNullOrEmpty(object value) => value == null;
        public List<PubScreenSearch> Search(PubScreen pubScreen)
        {
            var results = new List<PubScreenSearch>();
            //DisMaxQuery query = null;
            //QueryContainerDescriptor<PubScreen> pq = new QueryContainerDescriptor<PubScreen>();
            try
            {

                //if(pubScreen.search != null)
                //{
                //    query &= new MatchQuery()
                //    {
                //        Field = Field<PubScreenSearch>(p => p.Title),
                //        Query = pubScreen.search
                //    };
                //}
                var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
                    .Size(10000)
                    .Query(q => ApplyQuery(pubScreen, q)
                        )
                    );
                
                results = searchResult.Hits.Select(hit => hit.Source).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return results;
        }

    }


    internal class Program
    {
        private static string _cnnString_PubScreen = "Server=MOUSEBYTES;Database=PubScreen;Trusted_Connection=True;MultipleActiveResultSets=true";
        public static List<PubScreenSearch> GetPubscreenData()
        {
            List<PubScreenSearch> listOfPublication = new List<PubScreenSearch>();

            string query = "Select * From SearchPub";
            using (SqlConnection cn = new SqlConnection(_cnnString_PubScreen))
            {
                SqlCommand command = new SqlCommand(query, cn);
                cn.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var newPub = new PubScreenSearch();
                        newPub.ID = Int32.Parse(reader["ID"].ToString());
                        newPub.Title = Convert.ToString(reader["Title"].ToString());
                        newPub.Abstract = Convert.ToString(reader["Abstract"].ToString());
                        newPub.Keywords = Convert.ToString(reader["Keywords"].ToString());
                        newPub.DOI = Convert.ToString(reader["DOI"].ToString());
                        newPub.Year = Convert.ToString(reader["Year"].ToString());
                        newPub.Author = Convert.ToString(reader["Author"].ToString());
                        newPub.PaperType = Convert.ToString(reader["PaperType"].ToString());
                        newPub.Task = Convert.ToString(reader["Task"].ToString());
                        newPub.SubTask = Convert.ToString(reader["SubTask"].ToString());
                        newPub.Species = Convert.ToString(reader["Species"].ToString());
                        newPub.Sex = Convert.ToString(reader["Sex"].ToString());
                        newPub.Strain = Convert.ToString(reader["Strain"].ToString());
                        newPub.DiseaseModel = Convert.ToString(reader["DiseaseModel"].ToString());
                        newPub.BrainRegion = Convert.ToString(reader["BrainRegion"].ToString());
                        newPub.SubRegion = Convert.ToString(reader["SubRegion"].ToString());
                        newPub.CellType = Convert.ToString(reader["CellType"].ToString());
                        newPub.Method = Convert.ToString(reader["Method"].ToString());
                        newPub.SubMethod = Convert.ToString(reader["SubMethod"].ToString());
                        newPub.NeuroTransmitter = Convert.ToString(reader["NeuroTransmitter"].ToString());
                        newPub.Reference = Convert.ToString(reader["Reference"].ToString());
                        listOfPublication.Add(newPub);
                    }
                }

            }
            return listOfPublication;
        }





        static void Main(string[] args)
        {


            ElasticSearchPubScreen elasticPubscreen = new ElasticSearchPubScreen();
            elasticPubscreen.createIndices();
            var client = elasticPubscreen.GetElasticsearchClient();

          



            //var listPublication = GetPubscreenData();

            //foreach (var item in listPublication)
            //{
            //    try
            //    {

            //        var index = client.Index(item, c => c.Index("pubscreen")); ;
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}

            try
            {
                //var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
                //    .Size(10000)

                //.Query(q => q
                //    .DisMax(dx => dx
                //        .Queries(dxq => dxq
                //                    .Match(dxqm => dxqm
                //                    .Field(f => f.Title)
                //                    .Query(pubScreen.search)
                //                    .Fuzziness(new Fuzziness("Auto"))),
                //                dxq => dxq
                //                    .Match(dxqm => dxqm
                //                    .Field(f => f.Author)
                //                    .Query(pubScreen.search)
                //                    .Fuzziness(new Fuzziness("Auto"))),
                //                dxq => dxq
                //                    .Bool(boolq => boolq
                //                    .Should(boolShould => boolShould
                //                    .Wildcard(dxqm => dxqm
                //                    .Field(f => f.Keywords)
                //                    .Value("*" + pubScreen.search + "*")))),
                //                dxq => dxq
                //                    .Bool(boolq => boolq
                //                    .Should(boolShould => boolShould
                //                    .Wildcard(dxqm => dxqm
                //                    .Field(f => f.Title)
                //                    .Value("*" + pubScreen.search + "*")))),

                //                dxq => dxq
                //                    .Match(dxqm => dxqm
                //                    .Field(f => f.Author)
                //                    .Query(pubScreen.Author)
                //                    .Fuzziness(new Fuzziness("Auto")))
                ////.Match(m => m.Field(f => f.Title).Query("mouse")))

                //))));
                //var results = new List<PubScreenSearch>();
                //results = searchResult.Hits.Select(hit => hit.Source).ToList();
                //Console.WriteLine("Passed");
                //Console.ReadLine();"
                PubScreen testPubscreen = new PubScreen();
                //testPubscreen.Author = "Mathieu-Favier";

                testPubscreen.search = "mouse";
                Stopwatch sw = new Stopwatch();
                sw.Start();
                elasticPubscreen.Search(testPubscreen);
                sw.Stop();

                Console.WriteLine("elastSearch result time \tMilliseconds = {0},\tTicks = {1}", sw.ElapsedMilliseconds, sw.ElapsedTicks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

