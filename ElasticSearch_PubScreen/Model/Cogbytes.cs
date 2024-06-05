using System;
using System.Collections.Generic;

namespace ElasticSearch_PubScreen.Model
{
    public class Cogbytes
    {
        public int? ID { get; set; }
        public Guid RepoLinkGuid { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string Keywords { get; set; }
        public string DOI { get; set; }
        public int?[] AuthourID { get; set; }
        public string AuthorString { get; set; }
        public int?[] PIID { get; set; }
        public string PIString { get; set; }
        public string Link { get; set; }
        public bool PrivacyStatus { get; set; }
        public string Description { get; set; }
        public string AdditionalNotes { get; set; }
        public string DateRepositoryCreated { get; set; }
        //public List<Experiment> Experiment { get; set; }
        //public PubScreenSearch Paper { get; set; }
        public string DataCiteURL { get; set; }
    }
}
