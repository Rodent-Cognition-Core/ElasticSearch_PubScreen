﻿using Nest;
using System;
using System.Collections.Generic;

namespace ElasticSearch_PubScreen.Model
{
    [ElasticsearchType]
    public class PubScreenSearch
    {

        public int? ID { get; set; }
        public Guid PaperLinkGuid { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Keywords { get; set; }
        public string DOI { get; set; }
        public int? Year { get; set; }
        public string Author { get; set; }
        public string PaperType { get; set; }
        public string Task { get; set; }
        public string SubTask { get; set; }
        public string Species { get; set; }
        public string Sex { get; set; }
        public string Strain { get; set; }
        public string DiseaseModel { get; set; }
        public string SubModel { get; set; }
        public string BrainRegion { get; set; }
        public string SubRegion { get; set; }
        public string CellType { get; set; }
        public string Method { get; set; }
        public string SubMethod { get; set; }
        public string NeuroTransmitter { get; set; }
        public string Reference { get; set; }

        //public List<Experiment> Experiment { get; set; }
        //public List<Cogbytes> Repo { get; set; }

    }
}
