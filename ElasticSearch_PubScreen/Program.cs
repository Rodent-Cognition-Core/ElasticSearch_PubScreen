using AngularSPAWebAPI.Models;
using AngularSPAWebAPI.Services;
using CBAS.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Core.TermVectors;
using Elastic.Transport;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace ElasticSearch_PubScreen
{
    internal class ElasticSearchPubScreen
    {
        private static ElasticsearchClient client = null;
        public ElasticSearchPubScreen(ElasticsearchClientSettings setting) {
            
            
            try
            {
               client = new ElasticsearchClient(setting);
              
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error setting up elastic search, the following error occured " + ex.Message);
            }
            
        }
        public void createIndices()
        {

            var partialName = new CustomAnalyzer
            {
                Filter = new List<string> { "lowercase", "name_ngrams", "standard", "asciifolding", "trim", "word_delimiter" },
                Tokenizer = "lowercase"

            };

            var fullName = new CustomAnalyzer
            {
                Filter = new List<string> { "standard", "lowercase", "asciifolding", "word_delimiter" },
                Tokenizer = "lowercase"
            };

            client.Indices.Create("pubscreen", c => c

                .Settings(s => s
                    .Analysis(descriptor => descriptor
                        .TokenFilters(bases => bases
                        .NGram("name_ngrams", new NGramTokenFilter
                        {
                            MaxGram = 10,
                            MinGram = 2,
                        })
                        .Lowercase("partial_name")
                        )
                     )
                  )
               );
        }
        public ElasticsearchClient GetElasticsearchClient()
        {
            return client;
        }

        public List<PubScreenSearch> Search(string keyword)
        {
            keyword = keyword.ToLower();
            var results = new List<PubScreenSearch>();
            try
            {
                var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
                    .Size(10000)
                    .Query(q => q
                        .DisMax(dx => dx
                            .Queries(
                                dxq => dxq
                                    .Match(dxqm => dxqm
                                    .Field(f => f.Title)
                                    .Query(keyword)
                                    .Fuzziness(new Fuzziness("Auto"))),
                                dxq => dxq
                                    .Match(dxqm => dxqm
                                    .Field(f => f.Author)
                                    .Query(keyword)
                                    .Fuzziness(new Fuzziness("Auto"))),
                                dxq => dxq
                                    .Bool(boolq => boolq
                                    .Should(boolShould => boolShould
                                    .Wildcard(dxqm => dxqm
                                    .Field(f => f.Keywords)
                                    .Value("*" + keyword + "*")))),
                                dxq => dxq
                                    .Bool(boolq => boolq
                                    .Should(boolShould => boolShould
                                    .Wildcard(dxqm => dxqm
                                    .Field(f => f.Title)
                                    .Value("*" + keyword + "*"))))
                                    )
                            )
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
        private static string _cnnString_PubScreen = "Server=.\\sqlexpress;Database=PubScreen;Trusted_Connection=True;MultipleActiveResultSets=true";
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

        public List<PubScreenSearch> SearchPublications(PubScreen pubScreen)
        {
            List<PubScreenSearch> lstPubScreen = new List<PubScreenSearch>();

            string sql = "Select * From SearchPub Where ";

            if (!string.IsNullOrEmpty(pubScreen.search))
            {
                sql += $@"(SearchPub.Title like '%{(HelperService.EscapeSql(pubScreen.search)).Trim()}%' or
                           SearchPub.Keywords like '%{(HelperService.EscapeSql(pubScreen.search)).Trim()}%' or
                           SearchPub.Author like '%{(HelperService.EscapeSql(pubScreen.search)).Trim()}%') AND ";
            }
            // Title
            if (!string.IsNullOrEmpty(pubScreen.Title))
            {
                sql += $@"SearchPub.Title like '%{(HelperService.EscapeSql(pubScreen.Title)).Trim()}%' AND ";
            }

            //Keywords
            if (!string.IsNullOrEmpty(pubScreen.Keywords))
            {
                sql += $@"SearchPub.Keywords like '%{HelperService.EscapeSql(pubScreen.Keywords)}%' AND ";
            }

            // DOI
            if (!string.IsNullOrEmpty(pubScreen.DOI))
            {
                sql += $@"SearchPub.DOI = '{HelperService.EscapeSql(pubScreen.DOI)}' AND ";
            }



            // search query for Author
            if (pubScreen.AuthourID != null && pubScreen.AuthourID.Length != 0)
            {
                if (pubScreen.AuthourID.Length == 1)
                {
                    sql += $@"SearchPub.Author like '%'  + (Select CONCAT(Author.FirstName, '-', Author.LastName) From Author Where Author.ID = {pubScreen.AuthourID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.AuthourID.Length; i++)
                    {
                        sql += $@"SearchPub.Author like '%'  + (Select CONCAT(Author.FirstName, '-', Author.LastName) From Author Where Author.ID = {pubScreen.AuthourID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            // search query for Year
            if (pubScreen.YearFrom != null && pubScreen.YearTo != null)
            {
                sql += $@"(SearchPub.Year >= {pubScreen.YearFrom} AND SearchPub.Year <= {pubScreen.YearTo}) AND ";
            }

            if (pubScreen.YearFrom != null && pubScreen.YearTo == null)
            {
                sql += $@"(SearchPub.Year >= {pubScreen.YearFrom}) AND ";
            }

            if (pubScreen.YearTo != null && pubScreen.YearFrom == null)
            {
                sql += $@"(SearchPub.Year <= {pubScreen.YearTo}) AND ";
            }

            // search query for PaperType
            //if (pubScreen.PaperTypeID != null)
            //{
            //    sql += $@"SearchPub.PaperType like '%'  + (Select PaperType From PaperType Where PaperType.ID = {pubScreen.PaperTypeID}) +  '%' AND ";
            //}

            // search query for Paper type
            if (pubScreen.PaperTypeIdSearch != null && pubScreen.PaperTypeIdSearch.Length != 0)
            {

                if (pubScreen.PaperTypeIdSearch.Length == 1)
                {
                    sql += $@"SearchPub.PaperType like '%'  + (Select PaperType From PaperType Where PaperType.ID = {pubScreen.PaperTypeIdSearch[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.PaperTypeIdSearch.Length; i++)
                    {
                        sql += $@"SearchPub.PaperType like '%'  + (Select PaperType From PaperType Where PaperType.ID = {pubScreen.PaperTypeIdSearch[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            // search query for Task
            if (pubScreen.TaskID != null && pubScreen.TaskID.Length != 0)
            {

                if (pubScreen.TaskID.Length == 1)
                {
                    sql += $@"SearchPub.Task like '%'  + (Select Task From Task Where Task.ID = {pubScreen.TaskID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.TaskID.Length; i++)
                    {
                        sql += $@"SearchPub.Task like '%'  + (Select Task From Task Where Task.ID = {pubScreen.TaskID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            // search query for SubTask
            if (pubScreen.SubTaskID != null && pubScreen.SubTaskID.Length != 0)
            {
                if (pubScreen.SubTaskID.Length == 1)
                {
                    sql += $@"SearchPub.SubTask like '%'  + (Select SubTask From SubTask Where SubTask.ID = {pubScreen.SubTaskID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.SubTaskID.Length; i++)
                    {
                        sql += $@"SearchPub.SubTask like '%'  + (Select SubTask From SubTask Where SubTask.ID = {pubScreen.SubTaskID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            // search query for Species
            if (pubScreen.SpecieID != null && pubScreen.SpecieID.Length != 0)
            {
                if (pubScreen.SpecieID.Length == 1)
                {
                    sql += $@"SearchPub.Species like '%'  + (Select Species From Species Where Species.ID = {pubScreen.SpecieID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.SpecieID.Length; i++)
                    {
                        sql += $@"SearchPub.Species like '%'  + (Select Species From Species Where Species.ID = {pubScreen.SpecieID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Sex
            if (pubScreen.sexID != null && pubScreen.sexID.Length != 0)
            {
                if (pubScreen.sexID.Length == 1)
                {
                    sql += $@"SearchPub.Sex like '%'  + (Select Sex From Sex Where Sex.ID = {pubScreen.sexID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.sexID.Length; i++)
                    {
                        sql += $@"SearchPub.Sex like '%'  + (Select Sex From Sex Where Sex.ID = {pubScreen.sexID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Strain
            if (pubScreen.StrainID != null && pubScreen.StrainID.Length != 0)
            {
                if (pubScreen.StrainID.Length == 1)
                {
                    sql += $@"SearchPub.Strain like '%'  + (Select Strain From Strain Where Strain.ID = {pubScreen.StrainID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.StrainID.Length; i++)
                    {
                        sql += $@"SearchPub.Strain like '%'  + (Select Strain From Strain Where Strain.ID = {pubScreen.StrainID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Disease
            if (pubScreen.DiseaseID != null && pubScreen.DiseaseID.Length != 0)
            {
                if (pubScreen.DiseaseID.Length == 1)
                {
                    sql += $@"SearchPub.DiseaseModel like '%'  + (Select DiseaseModel From DiseaseModel Where DiseaseModel.ID = {pubScreen.DiseaseID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.DiseaseID.Length; i++)
                    {
                        sql += $@"SearchPub.DiseaseModel like '%'  + (Select DiseaseModel From DiseaseModel Where DiseaseModel.ID = {pubScreen.DiseaseID[i]}) +  '%' OR ";

                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Sub Model
            if (pubScreen.SubModelID != null && pubScreen.SubModelID.Length != 0)
            {
                if (pubScreen.SubModelID.Length == 1)
                {
                    sql += $@"SearchPub.SubModel like '%'  + (Select SubModel From SubModel Where SubModel.ID = {pubScreen.SubModelID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.SubModelID.Length; i++)
                    {
                        sql += $@"SearchPub.SubModel like '%'  + (Select SubModel From SubModel Where SubModel.ID = {pubScreen.SubModelID[i]}) +  '%' OR ";

                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for BrainRegion
            if (pubScreen.RegionID != null && pubScreen.RegionID.Length != 0)
            {
                if (pubScreen.RegionID.Length == 1)
                {
                    sql += $@"SearchPub.BrainRegion like '%'  + (Select BrainRegion From BrainRegion Where BrainRegion.ID = {pubScreen.RegionID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.RegionID.Length; i++)
                    {
                        sql += $@"SearchPub.BrainRegion like '%'  + (Select BrainRegion From BrainRegion Where BrainRegion.ID = {pubScreen.RegionID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for SubRegion
            if (pubScreen.SubRegionID != null && pubScreen.SubRegionID.Length != 0)
            {
                if (pubScreen.SubRegionID.Length == 1)
                {
                    sql += $@"SearchPub.SubRegion like '%'  + (Select SubRegion From SubRegion Where SubRegion.ID = {pubScreen.SubRegionID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.SubRegionID.Length; i++)
                    {
                        sql += $@"SearchPub.SubRegion like '%'  + (Select SubRegion From SubRegion Where SubRegion.ID = {pubScreen.SubRegionID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            // search query for CellType
            if (pubScreen.CellTypeID != null && pubScreen.CellTypeID.Length != 0)
            {
                if (pubScreen.CellTypeID.Length == 1)
                {
                    sql += $@"SearchPub.CellType like '%'  + (Select CellType From CellType Where CellType.ID = {pubScreen.CellTypeID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.CellTypeID.Length; i++)
                    {
                        sql += $@"SearchPub.CellType like '%'  + (Select CellType From CellType Where CellType.ID = {pubScreen.CellTypeID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Method
            if (pubScreen.MethodID != null && pubScreen.MethodID.Length != 0)
            {
                if (pubScreen.MethodID.Length == 1)
                {
                    sql += $@"SearchPub.Method like '%'  + (Select Method From Method Where Method.ID = {pubScreen.MethodID[0]}) +  '%' AND ";
                }

                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.MethodID.Length; i++)
                    {
                        sql += $@"SearchPub.Method like '%'  + (Select Method From Method Where Method.ID = {pubScreen.MethodID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Sub Method
            if (pubScreen.SubMethodID != null && pubScreen.SubMethodID.Length != 0)
            {
                if (pubScreen.SubMethodID.Length == 1)
                {
                    sql += $@"SearchPub.SubMethod like '%'  + (Select SubMethod From SubMethod Where SubMethod.ID = {pubScreen.SubMethodID[0]}) +  '%' AND ";

                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.SubMethodID.Length; i++)
                    {
                        sql += $@"SearchPub.SubMethod like '%'  + (Select SubMethod From SubMethod Where SubMethod.ID = {pubScreen.SubMethodID[i]}) +  '%' OR ";

                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }
            }

            // search query for Neuro Transmitter
            if (pubScreen.TransmitterID != null && pubScreen.TransmitterID.Length != 0)
            {
                if (pubScreen.TransmitterID.Length == 1)
                {
                    sql += $@"SearchPub.Neurotransmitter like '%'  + (Select Neurotransmitter From Neurotransmitter Where Neurotransmitter.ID = {pubScreen.TransmitterID[0]}) +  '%' AND ";
                }
                else
                {
                    sql += "(";
                    for (int i = 0; i < pubScreen.TransmitterID.Length; i++)
                    {
                        sql += $@"SearchPub.Neurotransmitter like '%'  + (Select Neurotransmitter From Neurotransmitter Where Neurotransmitter.ID = {pubScreen.TransmitterID[i]}) +  '%' OR ";
                    }
                    sql = sql.Substring(0, sql.Length - 3);
                    sql += ") AND ";
                }

            }

            sql = sql.Substring(0, sql.Length - 4); // to remvoe the last NAD from the query
            //sql += "ORDER BY Year DESC";

            string sqlMB = "";
            string sqlCog = "";
            List<Experiment> lstExperiment = new List<Experiment>();
            List<Cogbytes> lstRepo = new List<Cogbytes>();
            using (DataTable dt = Dal.GetDataTablePub(sql))
            {

                foreach (DataRow dr in dt.Rows)
                {
                    string doi = Convert.ToString(dr["DOI"].ToString());
                    if (String.IsNullOrEmpty(doi) == false)
                    {
                        sqlMB = $@"Select Experiment.*, Task.Name as TaskName From Experiment
                                   Inner join Task on Task.ID = Experiment.TaskID
                                   Where DOI = '{doi}'";

                        // empty lstExperiment list
                        lstExperiment.Clear();
                        using (DataTable dtExp = Dal.GetDataTable(sqlMB))
                        {
                            foreach (DataRow drExp in dtExp.Rows)
                            {

                                lstExperiment.Add(new Experiment
                                {
                                    ExpID = Int32.Parse(drExp["ExpID"].ToString()),
                                    ExpName = Convert.ToString(drExp["ExpName"].ToString()),
                                    StartExpDate = Convert.ToDateTime(drExp["StartExpDate"].ToString()),
                                    TaskName = Convert.ToString(drExp["TaskName"].ToString()),
                                    DOI = Convert.ToString(drExp["DOI"].ToString()),
                                    Status = Convert.ToBoolean(drExp["Status"]),
                                    TaskBattery = Convert.ToString(drExp["TaskBattery"].ToString()),

                                });
                            }

                        }

                        sqlCog = $"Select * From UserRepository Where DOI = '{doi}'";
                        lstRepo.Clear();
                        using (DataTable dtCog = Dal.GetDataTableCog(sqlCog))
                        {
                            foreach (DataRow drCog in dtCog.Rows)
                            {
                                var cogbytesService = new CogbytesService();
                                int repID = Int32.Parse(drCog["RepID"].ToString());
                                lstRepo.Add(new Cogbytes
                                {
                                    ID = repID,
                                    RepoLinkGuid = Guid.Parse(drCog["repoLinkGuid"].ToString()),
                                    Title = Convert.ToString(drCog["Title"].ToString()),
                                    Date = Convert.ToString(drCog["Date"].ToString()),
                                    Keywords = Convert.ToString(drCog["Keywords"].ToString()),
                                    DOI = Convert.ToString(drCog["DOI"].ToString()),
                                    Link = Convert.ToString(drCog["Link"].ToString()),
                                    PrivacyStatus = Boolean.Parse(drCog["PrivacyStatus"].ToString()),
                                    Description = Convert.ToString(drCog["Description"].ToString()),
                                    AdditionalNotes = Convert.ToString(drCog["AdditionalNotes"].ToString()),
                                    AuthourID = cogbytesService.FillCogbytesItemArray($"Select AuthorID From RepAuthor Where RepID={repID}", "AuthorID"),
                                    PIID = cogbytesService.FillCogbytesItemArray($"Select PIID From RepPI Where RepID={repID}", "PIID"),
                                });
                            }

                        }

                    }

                    lstPubScreen.Add(new PubScreenSearch
                    {
                        ID = Int32.Parse(dr["ID"].ToString()),
                        PaperLinkGuid = Guid.Parse(dr["PaperLinkGuid"].ToString()),
                        Title = Convert.ToString(dr["Title"].ToString()),
                        Keywords = Convert.ToString(dr["Keywords"].ToString()),
                        DOI = Convert.ToString(dr["DOI"].ToString()),
                        Year = Convert.ToString(dr["Year"].ToString()),
                        Author = Convert.ToString(dr["Author"].ToString()),
                        PaperType = Convert.ToString(dr["PaperType"].ToString()),
                        Task = Convert.ToString(dr["Task"].ToString()),
                        SubTask = Convert.ToString(dr["SubTask"].ToString()),
                        Species = Convert.ToString(dr["Species"].ToString()),
                        Sex = Convert.ToString(dr["Sex"].ToString()),
                        Strain = Convert.ToString(dr["Strain"].ToString()),
                        DiseaseModel = Convert.ToString(dr["DiseaseModel"].ToString()),
                        SubModel = Convert.ToString(dr["SubModel"].ToString()),
                        BrainRegion = Convert.ToString(dr["BrainRegion"].ToString()),
                        SubRegion = Convert.ToString(dr["SubRegion"].ToString()),
                        CellType = Convert.ToString(dr["CellType"].ToString()),
                        Method = Convert.ToString(dr["Method"].ToString()),
                        SubMethod = Convert.ToString(dr["SubMethod"].ToString()),
                        NeuroTransmitter = Convert.ToString(dr["NeuroTransmitter"].ToString()),
                        Reference = Convert.ToString(dr["Reference"].ToString()),
                        Experiment = new List<Experiment>(lstExperiment),
                        Repo = new List<Cogbytes>(lstRepo)

                    });
                    //lstExperiment.Clear();
                }
            }

            // search MouseBytes database to see if the dataset exists********************************************


            return lstPubScreen;


        }




        static void Main(string[] args)
        {
            var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
                            .CertificateFingerprint("b50bbd6388dae08d1cb7cdd026554001a8d06e516a2d0b88273bd4f02eb86622")
                            .Authentication(new BasicAuthentication("elastic", "R7*3Ufrfd_0e2ks4Nyww"));
            settings.DefaultIndex("pubscreen");

            ElasticSearchPubScreen elasticPubscreen = new ElasticSearchPubScreen(settings);
            var client = elasticPubscreen.GetElasticsearchClient();




            //var listPublication = GetPubscreenData();

            //foreach (var item in listPublication)
            //{
            //    try
            //    {

            //        var index = client.Index(item);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}

            //try
            //{
            //    var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
            //        .Size(10000)

            //    .Query(q => q
            //        .DisMax(dx => dx
            //            .Queries(dxq => dxq
            //                .Match(dxqm => dxqm
            //                .Field(f => f.Title)
            //                .Query("mouse")
            //                .Fuzziness(new Fuzziness("Auto"))
            //                ), dxq => dxq
            //                .Match(dxqm => dxqm
            //                .Field(f => f.Author)
            //                .Query("mouse")
            //                .Fuzziness(new Fuzziness("Auto"))),
            //                dxq => dxq
            //                .Bool(boolq => boolq
            //                .Should (boolShould => boolShould
            //                .Wildcard(dxqm => dxqm
            //                .Field(f => f.Keywords)
            //                .Value("*mouse*")/*.Fuzziness(new Fuzziness("Auto"))*/))
            //            ),dxq => dxq
            //            .Bool(boolq => boolq
            //                .Should(boolShould => boolShould
            //                .Wildcard(dxqm => dxqm
            //                .Field(f => f.Title)
            //                .Value("*mouse*")
            //                ))
            //            ))
            //    //.Match(m => m.Field(f => f.Title).Query("mouse")))

            //    )));
            //    var results = new List<PubScreenSearch>();
            //    results = searchResult.Hits.Select(hit => hit.Source).ToList();
            //    Console.WriteLine("Passed");
            //    Console.ReadLine();

            //}catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
        }
    }
}

