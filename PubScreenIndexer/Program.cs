using Nest;
using System;
using ElasticSearch_PubScreen.Model;
using System.Collections.Generic;
using ElasticSearch_PubScreen;
using System.Data.SqlClient;
using CBAS.Helpers;
using System.Data.SqlTypes;
using System.Text;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.Xml.Linq;
using ElasticSearch_PubScreen.Data;
using System.Data;
using Elastic.Clients.Elasticsearch.Sql;
using System.Linq;
using Elastic.Clients.Elasticsearch;

namespace PubScreenIndexer
{
    public class Program
    {
        private static ElasticClient Client { get; set; }
        private static List<PubScreenElasticSearchModel> GetPubscreenData()
        {
            List<PubScreenElasticSearchModel> listOfPublication = new List<PubScreenElasticSearchModel>();

            string query = "Select TOP (750) * From SearchPub";
            using (SqlConnection cn = new SqlConnection(ElasticSearchPubScreen.GetPubScreenConnectionString()))
            {
                SqlCommand command = new SqlCommand(query, cn);
                cn.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {

                    while (reader.Read())
                    {
                        var newPub = new PubScreenElasticSearchModel();

                        newPub.ID = (int)HelperService.ConvertToNullableInt(reader["ID"].ToString());
                        newPub.PaperLinkGuid = Guid.Parse(reader["PaperLinkGuid"].ToString());
                        newPub.Title = Convert.ToString(reader["Title"].ToString());
                        newPub.Abstract = Convert.ToString(reader["Abstract"].ToString());
                        newPub.Keywords = Convert.ToString(reader["Keywords"].ToString());
                        newPub.DOI = Convert.ToString(reader["DOI"].ToString());
                        newPub.Year = (int)HelperService.ConvertToNullableInt(reader["Year"].ToString());
                        
                        var names = Convert.ToString(reader["Author"].ToString());
                        newPub.Author = names.Split(",").ToList().ToArray();

                        var paperType = Convert.ToString(reader["PaperType"].ToString());
                        newPub.PaperType = paperType.Split(",").ToList().ToArray();

                        var task = Convert.ToString(reader["Task"].ToString());
                        newPub.Task = task.Split(",").ToList().ToArray();

                        var subTask = Convert.ToString(reader["SubTask"].ToString());
                        newPub.SubTask = subTask.Split(",").ToList().ToArray();
                        
                        var species = Convert.ToString(reader["Species"].ToString());
                        newPub.Species = species.Split(",").ToList().ToArray();
                       
                        var sex = Convert.ToString(reader["Sex"].ToString());
                        newPub.Sex = sex.Split(",").ToList().ToArray();

                        var strain = Convert.ToString(reader["Strain"].ToString());
                        newPub.Strain = strain.Split(",").ToList().ToArray();

                        var diseaseModel = Convert.ToString(reader["DiseaseModel"].ToString());
                        newPub.DiseaseModel = diseaseModel.Split(",").ToList().ToArray();

                        var subModel = Convert.ToString(reader["SubModel"]).ToString();
                        newPub.SubModel = subModel.Split(",").ToList().ToArray();

                        var brainRegion = Convert.ToString(reader["BrainRegion"].ToString());
                        newPub.BrainRegion = brainRegion.Split(",").ToList().ToArray();

                        var subRegion = Convert.ToString(reader["SubRegion"].ToString());
                        newPub.SubRegion = subRegion.Split(",").ToList().ToArray();

                        var cellType = Convert.ToString(reader["CellType"].ToString());
                        newPub.CellType = cellType.Split(",").ToList().ToArray();
                        
                        var method = Convert.ToString(reader["Method"].ToString());
                        newPub.Method = method.Split(",").ToList().ToArray();
                        
                        var subMethod = Convert.ToString(reader["SubMethod"].ToString());
                        newPub.SubMethod = subMethod.Split(",").ToList().ToArray();

                        var neuroTransmitter = Convert.ToString(reader["NeuroTransmitter"].ToString());
                        newPub. NeuroTransmitter = neuroTransmitter.Split(",").ToList().ToArray();

                        newPub.Reference = Convert.ToString(reader["Reference"].ToString());

                        listOfPublication.Add(newPub);
                    }
                }

            }
            return listOfPublication;
        }

        private static void AddItemToIndex(string index)
        {
            var listPublication = new List<PubScreenElasticSearchModel>();
            try
            {
                Console.WriteLine("Getting items to add to index");
                listPublication = GetPubscreenData();
            }
            catch(Exception e)
            {
                Console.WriteLine($"The following error occured when getting items to add to the index: {e.Message}");
            }

            Console.WriteLine($@"Indexing {listPublication.Count} items");
            foreach (var item in listPublication)
            {
                try
                {
                    Client.Index(item, c => c.Index(index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"The following erroor occured when indexing the items? {ex.Message}");
                }
            }
        }
        private static void CreateIndex(string index)
        {
            try
            {
                Console.WriteLine("Deleting exisitng index with same name as " + index);
                Client.Indices.Delete(new DeleteIndexRequest(Nest.Indices.Index(index)));

                Console.WriteLine("Creating indexing with name " + index);
                Client.Indices.Create(index, pub => pub
                .Settings(s => s
                    .Analysis(descriptor => descriptor
                        .Analyzers(analyzer => analyzer
                            .Custom("custom_analyzer", ca => ca
                            .Filters("lowercase", "stop", "classic", "word_delimiter")))
                        .TokenFilters(bases => bases
                            .EdgeNGram("custom_analyzer", td => td
                                .MinGram(2)
                                .MaxGram(25)
                                )
                            )
                        )
                    )
                .Map<PubScreenElasticSearchModel>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name(f => f.Author)
                            .Analyzer("custom_analyzer"))
                        .Text(t => t
                            .Name(f => f.Keywords)
                            .Analyzer("custom_analyzer"))
                        .Text(t => t
                            .Name(f => f.Title)
                            .Analyzer("custom_analyzer"))
                        .Text(t => t
                            .Name(f => f.Abstract)
                            .Analyzer("stop"))
                        .Keyword(t => t
                            .Name(f => f.DOI)
                            .Boost(1.0)
                            )
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void Main()
        {
            Client = new ElasticClient(ElasticSearchPubScreen.GetElasticsearchSettings());

            try
            {
                CreateIndex("pubscreen");
                Console.WriteLine("Index has been created");
                AddItemToIndex("pubscreen");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

    }
}
