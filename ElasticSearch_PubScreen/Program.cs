using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Transport;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace ElasticSearch_PubScreen
{


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

        static void Main(string[] args)
        {
            var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
                            .CertificateFingerprint("b50bbd6388dae08d1cb7cdd026554001a8d06e516a2d0b88273bd4f02eb86622")
                            .Authentication(new BasicAuthentication("elastic", "R7*3Ufrfd_0e2ks4Nyww"));
            //settings.DefaultIndex("pubscreen");

            ElasticsearchClient client = null;

            try
            {
                client = new ElasticsearchClient(settings);
                client.Indices.Create("pubscreen", c => c
                .Settings(s => s
                    .Analysis(a => a
                        .Analyzers(aa => aa
                            .Custom("pubscreenAnalyzer", pa => pa
                                .Tokenizer("pubscreenTokenizer")
                            )
                        )
                        .Tokenizers(p => p
                        .EdgeNGram("pubscreenTokenizer", ed => ed
                        .MinGram(2)
                        .MaxGram(8)
                        .TokenChars(new List<TokenChar>
                                                     {
                                                        TokenChar.Digit,
                                                        TokenChar.Symbol,
                                                        TokenChar.Punctuation,
                                                        TokenChar.Letter
                                                     }
                                    )
                                )
                            )

                        )
                    )

                );

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }




            //var listPublication = GetPubscreenData();

            //foreach(var item in listPublication)
            //{
            //    try
            //    {
            //        var index = client.Index(item);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message); 
            //    }


            try
            {
                var searchResult = client.Search<PubScreenSearch>(s => s.Index("pubscreen")
                    .Size(10000)

                .Query(q => q
                    .Bool(b => b
                    .Should(s => s
                    .Wildcard(sw => sw
                                    .Field(f => f.Title)
                                    .Value("*Mouse*")
                                    ), s => s
                                .Wildcard(sm => sm
                                    .Field(f => f.Author)
                                    .Value("*Mouse*")
                                    ), s => s
                                    .Wildcard(sk => sk
                                        .Field(f => f.Keywords)
                                        .Value("*Mouse*")))
                        )
                    )
                );
                //Console.WriteLine(searchResult);
                //client.Indices.Delete("pubscreen");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Passed");
            Console.ReadLine();

        }
    }
}
